using System;
using System.IO;
using Newtonsoft.Json;

// 세이브 파일을 임시 경로에 쓰고 검증하거나, 검증된 파일을 목적지로 옮기거나, 읽는다.
public class SaveIO
{
    private readonly JsonSerializerSettings jsonSettings;
    private readonly SaveCheck saveCheck;

    // JSON 변환 규칙과 저장 파일 검사기를 받는다.
    public SaveIO(SaveCheck saveCheck)
    {
        this.saveCheck = saveCheck;
        jsonSettings = new JsonSerializerSettings();
        jsonSettings.Converters.Add(new CellConverter());
    }

    // 파일을 임시 경로에 쓰고, 다시 읽어 검증까지 통과하는지 확인한다 (아직 정식 파일로 확정하지 않음).
    public bool TryWriteTemp(string tempPath, SaveFile saveFile)
    {
        try
        {
            string json = JsonConvert.SerializeObject(saveFile, jsonSettings);
            WriteTemp(tempPath, json);

            SaveFile readBack = LoadFile(tempPath);
            return saveCheck.IsValidSave(readBack, saveFile.slotId, saveFile.saveOrder);
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

    // 지정한 경로의 저장 파일 하나를 읽는다.
    public bool TryRead(string filePath, out SaveFile saveFile)
    {
        try
        {
            saveFile = LoadFile(filePath);
            return true;
        }
        catch (IOException)
        {
            saveFile = null;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            saveFile = null;
            return false;
        }
        catch (JsonException)
        {
            saveFile = null;
            return false;
        }
    }

    // 파일을 목적지 경로로 옮긴다. 목적지에 이미 파일이 있으면 교체하고, 없으면 그대로 옮긴다.
    public void MoveOverwrite(string sourcePath, string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(sourcePath, destinationPath, null);
            return;
        }

        File.Move(sourcePath, destinationPath);
    }

    // JSON 문자열을 임시 파일에 쓰고 디스크까지 반영한다.
    private void WriteTemp(string filePath, string json)
    {
        using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            writer.Write(json);
            writer.Flush();
            stream.Flush(true);
        }
    }

    // 저장 파일을 JSON에서 복원한다.
    private SaveFile LoadFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<SaveFile>(json, jsonSettings);
    }
}
