using UnityEngine;
using Newtonsoft.Json;

public class JsonTest : MonoBehaviour
{
    void Start()
    {
        TestData data = new TestData();

        data.name = "≤‚ ‘";
        data.value = 10;

        string json = JsonConvert.SerializeObject(data);

        Debug.Log(json);
    }
}

public class TestData
{
    public string name;
    public int value;
}