using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class AgentData
{
    public List<Vector3> positions;
}

public class TrashcanData : AgentData
{
    public TrashcanData() {}
    public TrashcanData(string param) {
        positions = new List<Vector3>();
        stacks = new List<int>();
    }
    public List<int> stacks;
}

public class AgentController : MonoBehaviour
{
    string serverUrl = "http://localhost:8585";
    string getAgentsEndpoint = "/getAgents";
    string getObstaclesEndpoint = "/getObstacles";
    string getTrashcanEndpoint = "/getTrashcan";
    string getBoxEndpoint = "/getBox";
    string sendConfigEndpoint = "/init";
    string updateEndpoint = "/update";
    string boxDeleteEndpoint = "/boxDelete";

    AgentData robotData, obstacleData, boxData;
    TrashcanData trashcans, trashcanData;
    GameObject[] agents;
    // GameObject[] boxes;
    List<GameObject> boxes;
    GameObject[] trashCan;

    bool[] agentHasBox;

    List<Vector3> oldPositions;
    List<Vector3> newPositions;

    public GameObject obstaclePrefab, roombaPrefab, boxPrefab, floor;
    public int density, nBoxes, NAgents, width, height, maxSteps;
    public float timeToUpdate = 5.0f, timer, dt;
    bool hold = false;


    void Start()
    {
        robotData = new AgentData();
        obstacleData = new AgentData();
        oldPositions = new List<Vector3>();
        newPositions = new List<Vector3>();
        boxes = new List<GameObject>();

        trashcans = new TrashcanData("init");

        agents = new GameObject[NAgents];
        agentHasBox = new bool[NAgents];
        // boxes = new GameObject[nBoxes];
        

        floor.transform.localScale = new Vector3((float)width/10, 1, (float)height/10);
        floor.transform.localPosition = new Vector3((float)width/2-0.5f, 0.5f, (float)height/2-0.5f);

        timer = timeToUpdate;

        for(int i = 0; i < NAgents; i++)
            agents[i] = Instantiate(roombaPrefab, Vector3.zero, Quaternion.identity);
        
    
            
        StartCoroutine(SendConfiguration()); 
    }

    IEnumerator SendConfiguration()
    {
        WWWForm form = new WWWForm();

        form.AddField("numAgents", NAgents.ToString());
        form.AddField("nBoxes", nBoxes.ToString());
        form.AddField("width", width.ToString());
        form.AddField("density", density.ToString());
        form.AddField("width", width.ToString());
        form.AddField("height", height.ToString());
        form.AddField("maxSteps", maxSteps.ToString());

        UnityWebRequest www = UnityWebRequest.Post(serverUrl + sendConfigEndpoint, form);
        www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Configuration upload complete!");
            Debug.Log("Getting Agents positions");
            StartCoroutine(GetRobotData());
            StartCoroutine(GetObstacleData());
            StartCoroutine(GetBoxData("INIT"));
        }
    }

    private void Update() 
    {
        float t = timer/timeToUpdate;
        UnityWebRequest www;
        // Smooth out the transition at start and end
        dt = t * t * ( 3f - 2f*t);

        if(timer >= timeToUpdate)
        {
            timer = 0;
            hold = true;
            StartCoroutine(UpdateSimulation());
        }

        if (!hold)
        {
            UnityWebRequest mywww;

            for (int s = 0; s < agents.Length; s++)
            {
                // www = UnityWebRequest.Get(serverUrl + getAgentMoveEndpoint);

                Vector3 interpolated = Vector3.Lerp(oldPositions[s], newPositions[s], dt);
                Vector3 newPosition;

                agents[s].transform.localPosition = interpolated;
                
                Vector3 dir = oldPositions[s] - newPositions[s];
                agents[s].transform.rotation = Quaternion.LookRotation(dir);
            }
            // Move time from the last frame
            timer += Time.deltaTime;
        }
    }

    IEnumerator UpdateSimulation()
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + updateEndpoint);
        
        StartCoroutine(GetBoxData("UPDATE"));
        StartCoroutine(GetTrashcanData());
        
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            StartCoroutine(GetRobotData());
        }
    }

    IEnumerator GetRobotData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getAgentsEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            robotData = JsonUtility.FromJson<AgentData>(www.downloadHandler.text);

            // Store the old positions for each agent
            oldPositions = new List<Vector3>(newPositions);

            newPositions.Clear();

            foreach(Vector3 v in robotData.positions)
                newPositions.Add(v);

            hold = false;
        }
    }

    IEnumerator GetObstacleData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getObstaclesEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            obstacleData = JsonUtility.FromJson<AgentData>(www.downloadHandler.text);

            Debug.Log(obstacleData.positions);

            foreach(Vector3 position in obstacleData.positions)
            {
                Instantiate(obstaclePrefab, position, Quaternion.identity);
            }
        }
    }

    IEnumerator GetBoxData(string param) 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getBoxEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            boxData = JsonUtility.FromJson<AgentData>(www.downloadHandler.text);

            Debug.Log(boxData.positions);
            
            if (param == "INIT") {
                for(int i = 0; i < boxData.positions.Count; i++)
                {
                    boxes.Add(Instantiate(boxPrefab, boxData.positions[i], Quaternion.identity));
                }
            }

            Debug.Log("POSITIONS: " + boxData.positions.Count.ToString());
            Debug.Log("BOXES: " + boxes.Count.ToString());

            if (param == "UPDATE") {
                for (int i = 0; i < boxData.positions.Count; i++) {
                    if (!boxData.positions.Contains(boxes[i].transform.position)) {
                        boxes[i].SetActive(false);
                        boxes.RemoveAt(i);
                        Debug.Log("CAJA ELIMINADA");
                        Debug.Log(boxes[i].transform.position);
                    }
                }
            }
        }
            
    }

    IEnumerator GetTrashcanData() 
    {
        UnityWebRequest www = UnityWebRequest.Get(serverUrl + getTrashcanEndpoint);
        yield return www.SendWebRequest();
 
        if (www.result != UnityWebRequest.Result.Success)
            Debug.Log(www.error);
        else 
        {
            trashcanData = JsonUtility.FromJson<TrashcanData>(www.downloadHandler.text);

            for(int i = 0; i < trashcanData.positions.Count; i++)
            {
                if (!trashcans.positions.Contains(trashcanData.positions[i])) {
                    trashcans.positions.Add(trashcanData.positions[i]);
                    trashcans.stacks.Add(0);
                }

                if (trashcans.stacks[i] < trashcanData.stacks[i]) {
                    for (int j = trashcans.stacks[i]; j < trashcanData.stacks[i]-trashcans.stacks[i]; j++) {
                        Instantiate(boxPrefab, new Vector3(trashcanData.positions[i][0], trashcanData.positions[i][1]+j, trashcanData.positions[i][2]), Quaternion.identity);
                    }

                    trashcans.stacks[i] = trashcanData.stacks[i];
                }
            }
        }
            
    }

}
