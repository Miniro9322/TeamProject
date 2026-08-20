using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

// 저장 파일의 체크섬을 계산하고 식별 정보와 손상 여부를 검사한다.
public class SaveCheck
{
    public const string FileTag = "GYARK_SAVE";
    public const int SaveVersion = 1;

    private readonly JsonSerializerSettings jsonSettings;

    // 체크섬 계산에 사용할 JSON 변환 규칙을 준비한다.
    public SaveCheck()
    {
        jsonSettings = new JsonSerializerSettings();
        jsonSettings.Converters.Add(new CellConverter());
    }

    // 저장 데이터의 SHA-256 체크섬을 계산한다.
    public string GetHash(SaveData saveData)
    {
        string json = JsonConvert.SerializeObject(saveData, jsonSettings);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    // 저장 파일의 식별자·버전·슬롯·체크섬을 검사한다 (순번은 보지 않음 — 읽을 때는 기대 순번이 없음).
    public bool IsValidSave(SaveFile saveFile, int slotId)
    {
        if (IsMissing(saveFile))
        {
            return false;
        }

        if (HasWrongTag(saveFile))
        {
            return false;
        }

        if (HasWrongVersion(saveFile))
        {
            return false;
        }

        if (HasWrongSlot(saveFile, slotId))
        {
            return false;
        }

        return HasWrongHash(saveFile) == false;
    }

    // 저장 파일의 식별자·버전·슬롯·순번·체크섬을 검사한다 (쓰기 직후 자기검증 전용 — 방금 쓰려던 순번과 비교).
    public bool IsValidSave(SaveFile saveFile, int slotId, int saveOrder)
    {
        if (!IsValidSave(saveFile, slotId))
        {
            return false;
        }

        return HasWrongOrder(saveFile, saveOrder) == false;
    }

    // 저장 파일이 비어 있는지 검사한다.
    private bool IsMissing(SaveFile saveFile)
    {
        return saveFile == null;
    }

    // 저장 파일 식별자가 다른지 검사한다.
    private bool HasWrongTag(SaveFile saveFile)
    {
        return saveFile.fileTag != FileTag;
    }

    // 저장 구조 버전이 다른지 검사한다.
    private bool HasWrongVersion(SaveFile saveFile)
    {
        return saveFile.saveVersion != SaveVersion;
    }

    // 요청한 슬롯 번호와 다른지 검사한다.
    private bool HasWrongSlot(SaveFile saveFile, int slotId)
    {
        return saveFile.slotId != slotId;
    }

    // 요청한 저장 순번과 다른지 검사한다.
    private bool HasWrongOrder(SaveFile saveFile, int saveOrder)
    {
        return saveFile.saveOrder != saveOrder;
    }

    // 저장 데이터의 체크섬이 다른지 검사한다.
    private bool HasWrongHash(SaveFile saveFile)
    {
        return saveFile.checkSum != GetHash(saveFile.saveData);
    }
}
