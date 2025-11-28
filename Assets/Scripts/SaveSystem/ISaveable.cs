public interface ISaveableV2
{
    string GetUniqueID();
    ObjectSaveData GetCommonSaveData();
    void LoadCommonData(ObjectSaveData data);
}

public interface IHasMineralData : ISaveableV2
{
    MineralSaveData GetMineralSaveData();
    void LoadMineralData(MineralSaveData data);
}

public interface IHasDepositData : ISaveableV2
{
    DepositSaveData GetDepositSaveData();
    void LoadDepositData(DepositSaveData data);
}