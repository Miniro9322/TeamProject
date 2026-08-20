using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// 슬롯의 저장 경로를 관리하고, 세대 2개(Save_0/Save_1)를 번갈아 덮어써 손상에 대비한다.
public class SaveSlot
{
    private const string RootName = "Saves";
    private const string SlotPrefix = "Slot_";
    private const string SlotFormat = "D2";
    private const string FilePrefix = "Save_";
    private const string TempExt = ".tmp";
    private const string SaveExt = ".sav";
    private const int GenerationCount = 2;

    private readonly SaveIO saveIO;
    private readonly SaveCheck saveCheck;

    // 파일 처리 담당자와 저장 파일 검사기를 받는다.
    public SaveSlot(SaveIO saveIO, SaveCheck saveCheck)
    {
        this.saveIO = saveIO;
        this.saveCheck = saveCheck;
    }

    // 두 세대 중 더 오래된 쪽에 새 저장본을 덮어쓴다.
    public bool TryWrite(int slotId, SaveFile saveFile)
    {
        string folder = SlotPath(slotId);

        try
        {
            Directory.CreateDirectory(folder);

            int latestOrder = LatestOrder(folder, out int latestGeneration);
            int targetGeneration = OtherGeneration(latestGeneration);
            int newOrder = latestOrder + 1;

            SaveFile output = CreateFile(slotId, newOrder, saveFile);
            string tempPath = GenerationPath(folder, targetGeneration, TempExt);
            string finalPath = GenerationPath(folder, targetGeneration, SaveExt);

            return saveIO.TryWrite(tempPath, finalPath, output);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // 두 세대 중 검증을 통과하는 가장 최신 저장본을 읽는다.
    public bool TryReadLatest(int slotId, out SaveFile saveFile)
    {
        string folder = SlotPath(slotId);
        SaveFile best = null;
        int bestOrder = -1;

        for (int generation = 0; generation < GenerationCount; generation++)
        {
            if (!TryReadValid(folder, slotId, generation, out SaveFile candidate)) continue;
            if (candidate.saveOrder <= bestOrder) continue;

            best = candidate;
            bestOrder = candidate.saveOrder;
        }

        saveFile = best;
        return best != null;
    }

    // 지정한 세대 파일을 읽고 검증까지 통과하는지 본다.
    private bool TryReadValid(string folder, int slotId, int generation, out SaveFile saveFile)
    {
        string filePath = GenerationPath(folder, generation, SaveExt);
        if (!saveIO.TryRead(filePath, out saveFile)) return false;
        return saveCheck.IsValidSave(saveFile, slotId, saveFile.saveOrder);
    }

    // 전달받은 값에 슬롯 정보와 체크섬을 붙인다.
    private SaveFile CreateFile(int slotId, int saveOrder, SaveFile saveFile)
    {
        return new SaveFile
        {
            fileTag = saveFile.fileTag,
            saveVersion = saveFile.saveVersion,
            slotId = slotId,
            saveOrder = saveOrder,
            checkSum = saveCheck.GetHash(saveFile.saveData),
            saveData = saveFile.saveData
        };
    }

    // 두 세대 파일 중 가장 높은 저장 순번과, 그 세대 번호를 찾는다 (둘 다 없으면 순번 0·세대 0).
    private int LatestOrder(string folder, out int latestGeneration)
    {
        latestGeneration = 0;
        int latestOrder = 0;

        for (int generation = 0; generation < GenerationCount; generation++)
        {
            if (!saveIO.TryRead(GenerationPath(folder, generation, SaveExt), out SaveFile file)) continue;
            if (file.saveOrder <= latestOrder) continue;

            latestOrder = file.saveOrder;
            latestGeneration = generation;
        }

        return latestOrder;
    }

    // 세대가 0·1 두 개뿐이므로 반대쪽 세대 번호를 돌려준다.
    private int OtherGeneration(int generation)
    {
        return (GenerationCount - 1) - generation;
    }

    // 슬롯 번호에 맞는 폴더 경로를 만든다.
    private string SlotPath(int slotId)
    {
        string root = Path.Combine(Application.persistentDataPath, RootName);
        string slotName = SlotPrefix + slotId.ToString(SlotFormat);
        return Path.Combine(root, slotName);
    }

    // 세대 번호와 확장자에 맞는 파일 경로를 만든다.
    private string GenerationPath(string folder, int generation, string extension)
    {
        string fileName = FilePrefix + generation + extension;
        return Path.Combine(folder, fileName);
    }
}
