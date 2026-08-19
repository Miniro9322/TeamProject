using System;
using System.IO;
using Newtonsoft.Json;

// 세이브 파일 하나를 임시로 쓰고 검증한 뒤 정상 파일로 확정하거나 읽는다.
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

    // 파일 하나를 임시로 쓰고 검증한 뒤 정상 파일로 확정한다.
    public bool TryWrite(string tempPath, string finalPath, SaveFile saveFile)
    {
        try
        {
            string json = JsonConvert.SerializeObject(saveFile, jsonSettings);
            WriteTemp(tempPath, json);

            SaveFile verify = LoadFile(tempPath);
            if (!saveCheck.IsValidSave(verify, saveFile.slotId, saveFile.saveOrder))
            {
                return false;
            }

            File.Move(tempPath, finalPath);
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
