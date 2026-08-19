using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

// 슬롯의 저장 경로·순번·파일 목록과 이전 저장본 정리를 관리한다.
public class SaveSlot
{
    private const string RootName = "Saves";
    private const string SlotPrefix = "Slot_";
    private const string SlotFormat = "D2";
    private const string FilePrefix = "Save_";
    private const string OrderFormat = "D6";
    private const string TempExt = ".tmp";
    private const string SaveExt = ".sav";
    private const int KeepCount = 2;

    private readonly SaveIO saveIO;
    private readonly SaveCheck saveCheck;

    // 파일 처리 담당자와 저장 파일 검사기를 받는다.
    public SaveSlot(SaveIO saveIO, SaveCheck saveCheck)
    {
        this.saveIO = saveIO;
        this.saveCheck = saveCheck;
    }

    // 슬롯의 다음 순번으로 새 정상 저장본을 만든다.
    public bool TryWrite(int slotId, SaveFile saveFile)
    {
        string folder = SlotPath(slotId);

        try
        {
            Directory.CreateDirectory(folder);

            int saveOrder = NextOrder(folder);
            SaveFile output = CreateFile(slotId, saveOrder, saveFile);
            string tempPath = SavePath(folder, saveOrder, TempExt);
            string finalPath = SavePath(folder, saveOrder, SaveExt);

            if (!saveIO.TryWrite(tempPath, finalPath, output))
            {
                return false;
            }

            CleanOld(folder);
            return true;
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

    // 슬롯의 특정 순번 저장 파일을 읽는다.
    public bool TryRead(int slotId, int saveOrder, out SaveFile saveFile)
    {
        string folder = SlotPath(slotId);
        string filePath = SavePath(folder, saveOrder, SaveExt);
        return saveIO.TryRead(filePath, out saveFile);
    }

    // 슬롯의 저장 순번을 최신순으로 반환한다.
    public bool TryOrders(int slotId, out int[] orders)
    {
        try
        {
            orders = ReadOrders(SlotPath(slotId));
            Array.Sort(orders);
            Array.Reverse(orders);
            return true;
        }
        catch (IOException)
        {
            orders = Array.Empty<int>();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            orders = Array.Empty<int>();
            return false;
        }
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

    // 기존 저장 순번의 다음 번호를 계산한다.
    private int NextOrder(string folder)
    {
        int[] orders = ReadOrders(folder);
        if (orders.Length == 0)
        {
            return 1;
        }

        Array.Sort(orders);
        return orders[orders.Length - 1] + 1;
    }

    // 슬롯 폴더의 정상 저장 파일 순번을 읽는다.
    private int[] ReadOrders(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return Array.Empty<int>();
        }

        string pattern = FilePrefix + "*" + SaveExt;
        string[] files = Directory.GetFiles(folder, pattern);
        List<int> orderList = new List<int>(files.Length);

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string orderText = fileName.Substring(FilePrefix.Length);
            if (int.TryParse(orderText, out int order))
            {
                orderList.Add(order);
            }
        }

        return orderList.ToArray();
    }

    // 정상 저장본 두 개를 남기고 오래된 파일을 지운다.
    private void CleanOld(string folder)
    {
        try
        {
            int[] orders = ReadOrders(folder);
            Array.Sort(orders);
            int removeCount = Math.Max(0, orders.Length - KeepCount);

            for (int index = 0; index < removeCount; index++)
            {
                string filePath = SavePath(folder, orders[index], SaveExt);
                File.Delete(filePath);
            }
        }
        catch (IOException error)
        {
            Debug.LogWarning($"오래된 세이브 파일을 정리하지 못했습니다: {error.Message}");
        }
        catch (UnauthorizedAccessException error)
        {
            Debug.LogWarning($"오래된 세이브 파일을 정리하지 못했습니다: {error.Message}");
        }
    }

    // 슬롯 번호에 맞는 폴더 경로를 만든다.
    private string SlotPath(int slotId)
    {
        string root = Path.Combine(Application.persistentDataPath, RootName);
        string slotName = SlotPrefix + slotId.ToString(SlotFormat);
        return Path.Combine(root, slotName);
    }

    // 저장 순번과 확장자에 맞는 파일 경로를 만든다.
    private string SavePath(string folder, int saveOrder, string extension)
    {
        string fileName = FilePrefix + saveOrder.ToString(OrderFormat) + extension;
        return Path.Combine(folder, fileName);
    }
}
