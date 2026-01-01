using UnityEngine;
using SimpleJSON;

public class NetworkPlayer : MonoBehaviour
{
    private PlayerData _playerData;
    public PlayerData playerData => _playerData ??= this.GetComponent<PlayerData>();
    float _lastUpdateTime;

    void Awake()
    {
        this._playerData = this.GetComponent<PlayerData>();
    }

    void Update()
    {

    }

    public void UpdateState(string json)
    {
        var data = JSON.Parse(json);
        JSONArray array = data.AsArray;
        foreach (JSONNode item in array)
        {
            if (item["id"].AsInt == playerData.ID)
            {
                var pos = item["dirtyState"]["position"];
                float x = pos["x"].AsFloat;
                float y = pos["y"].AsFloat;
                float z = pos["z"].AsFloat;
                this.transform.position = new Vector3(x, y, z);

            }
        }
    }
}