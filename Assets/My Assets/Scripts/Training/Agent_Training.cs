using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class Agent_Training : Agent
{
    public int agentID;
    [Header("Movement")]
    //Agent's Parameters.
    public float turnSpeed = 150f;
    public float moveSpeed = 0.1f;
    private Rigidbody AgentRb;
    private Vector3 startingPos;
    private Vector3 goalPos;
    private float goalDistance;
    [Header("Reward Parameters")]
    public float currentGoalDistance;
    public float currentAngle;
    private Vector3 goalVector;
    private int stillCounter;
    private int rotationCounter;
    //General Parameters
    public float reward;
    private GameObject[] spawnAreas;
    private List<Monitor_Training.GoalAndSpawn> goals;
    private Transform agentParent;
    private List<GameObject> agents;
    public GameObject closestAgent;
    private Vector3 groupCenterPoint;
    public int closestAgentID;
    private List<GameObject> interactionObjects;
    private GameObject closestInteraction;
    public float closeAgents = 0;
    public bool initial = true;
    //Weights Manager
    Monitor_Training manager;
    //Save Route
    List<float[]> route;
    private int countEpisode;
    private int localPhase = 1;
    public float goalWeight;
    public float collWeight;
    public float groupWeight;
    public float interWeight;
    private bool inWeightRegion;
    private bool localOppositeGoal = false;
    private int collisionsCount = 0;
    public GameObject[] weightRegionColliders;
    private float[] startingWeights;

    private void Awake()
    {
        this.agentID = (int) transform.position.y;
    }

    //Run only one time once scene starts
    public override void Initialize()
    {
        this.inWeightRegion = false;
        this.AgentRb = this.GetComponent<Rigidbody>();
        this.manager = GameObject.Find("Environment").GetComponent<Monitor_Training>();
        if (this.manager.saveRoutes)
            this.route = new List<float[]>();
        this.agentParent = GameObject.Find("Agents").transform;
        this.agents = new List<GameObject>();
        this.interactionObjects = new List<GameObject>();
        //Get goals areas in scene
        this.goals = this.manager.getGoalAreas();
        this.weightRegionColliders = GameObject.FindGameObjectsWithTag("WeightsCollider");
    }

    //Run every time a new episode starts
    public override void OnEpisodeBegin()
    {
        if (setMultiBehaviourWeights())
            this.localOppositeGoal = true;
        this.inWeightRegion = false;
        this.startingWeights = new float[] { this.manager.goalMax, 2.0f, this.manager.interMin, -3.0f };
        // If first time initalize a spawn and a goal point
        if (this.initial || this.manager.disappearOnGoal == true)
        {
            this.initial = false;
            //Get a random agent spawn point and rotate agent to look at target
            Vector3[] generatedPoints = randomSpawnPoints();
            this.startingPos = generatedPoints[0];
            this.goalPos = generatedPoints[1];
            transform.position = this.startingPos;
            transform.LookAt(this.goalPos);
            if (this.manager.saveRoutes)
                this.route.Clear();
        }
        // If not first time keep current position as spawn point and get only a new goal point
        else
        {
            this.startingPos = transform.position;
            Vector3[] generatedPoints = randomSpawnPoints();
            this.goalPos = generatedPoints[1];
            while (Vector3.Distance(this.startingPos, this.goalPos) < 40f)
            {
                generatedPoints = randomSpawnPoints();
                this.goalPos = generatedPoints[1];
            }
        }
        transform.GetComponent<TrailRenderer>().Clear();
        this.goalDistance = Vector3.Distance(transform.position, this.goalPos);
        this.stillCounter = 0;
    }

    private bool checkInWeightRegion()
    {
        float weightsDistance = this.manager.inheritWeightsDistance;
        if (this.manager.multiBehaviors)
        {
            if (Vector3.Distance(transform.position, Vector3.zero) <= weightsDistance)
                return true;
            Vector3[] squaresCenters = { new Vector3(30f, 0f, 30f), new Vector3(-30f, 0f, 30f), new Vector3(30f, 0f, -30f), new Vector3(-30f, 0f, -30f) };
            float squareRadius = 19f;
            for (int i = 0; i < squaresCenters.Length; i++)
            {
                if (Vector3.Distance(transform.position, squaresCenters[i]) < squareRadius)
                    return true;
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, Vector3.zero) <= weightsDistance)
                return true;
            try
            {
                Vector3 currentPos = new Vector3(transform.position.x, this.weightRegionColliders[0].transform.position.y, transform.position.z);
                foreach (GameObject g in this.weightRegionColliders)
                    if (g.GetComponent<Collider>().bounds.Contains(currentPos))
                        return true;
            }
            catch { }
        }
        return false;
    }

    private bool setMultiBehaviourWeights()
    {
        bool isGoal = false;

        float goalAgents = (int)(this.manager.goalPercentage * this.manager.numOfAgents) / 100;
        float groupAgents = (int)(this.manager.groupPercentage * this.manager.numOfAgents) / 100;
        float interactAgents = (int)(this.manager.interactionPercentage * this.manager.numOfAgents) / 100;

        List<float[]> ratios = new List<float[]>();
        ratios.Add(new float[] { goalAgents, 1.8f, 0.5f, -2f, 0.1f });
        ratios.Add(new float[] { interactAgents, 0.1f, 1.0f, 5f, 1f });
        ratios.Add(new float[] { groupAgents, 0.1f, 1.0f, -1f, 5f });
        ratios.Sort((p1, p2) => p1[0].CompareTo(p2[0]));

        //Try multible behaviours in same type
        if (this.agentID < ratios[0][0])
        {
            this.goalWeight = ratios[0][1];
            this.collWeight = ratios[0][2];
            this.interWeight = ratios[0][3];
            this.groupWeight = ratios[0][4];
            if (ratios[0][1] > 1.7f)
                isGoal = true;
        }
        else if (this.agentID < (ratios[0][0] + ratios[1][0]))
        {
            this.goalWeight = ratios[1][1];
            this.collWeight = ratios[1][2];
            this.interWeight = ratios[1][3];
            this.groupWeight = ratios[1][4];
            if (ratios[1][1] > 1.7f)
                isGoal = true;
        }
        else
        {
            this.goalWeight = ratios[2][1];
            this.collWeight = ratios[2][2];
            this.interWeight = ratios[2][3];
            this.groupWeight = ratios[2][4];
            if (ratios[2][1] > 1.7f)
                isGoal = true;
        }
        return isGoal;
    }

    //Below code is for saving routes to csv
    private void Update()
    {
        if (checkInWeightRegion() == true)
        {
            this.inWeightRegion = true;
            StartCoroutine(changeWeights());
        }
        else
        {
            if (this.manager.keepInheritWeights == false)
            {
                float weightsDistance = this.manager.inheritWeightsDistance;
                if (Vector3.Distance(transform.position, Vector3.zero) > weightsDistance)
                    this.inWeightRegion = false;
            }
        }

        if (this.manager.demoScenes == false) {
            this.goalWeight = this.manager.goalWeight;
            this.collWeight = this.manager.collisionWeight;
            this.interWeight = this.manager.interactWeight;
            this.groupWeight = this.manager.groupWeight;
        }
        else
        {
            if(this.inWeightRegion == false)
            {
                this.goalWeight = this.startingWeights[0];
                this.collWeight = this.startingWeights[1];
                this.interWeight = this.startingWeights[2];
                this.groupWeight = this.startingWeights[3];
            }
            else
            {
                if (this.manager.multiBehaviors == true)
                {
                    setMultiBehaviourWeights();
                }
                else
                {
                    this.goalWeight = this.manager.goalWeight;
                    this.collWeight = this.manager.collisionWeight;
                    this.interWeight = this.manager.interactWeight;
                    this.groupWeight = this.manager.groupWeight;
                }
            }
        }

        if (this.localPhase != this.manager.phase)
        {
            SetReward(0f);
            this.localPhase = this.manager.phase;
        }
        if (this.manager.saveRoutes && this.manager.stopSaving)
        {
            EpisodeEnded();
            Destroy(this.gameObject);
        }
        this.reward = GetCumulativeReward();
    }

    // If GridSpawn is enabled, return points in 2d grid
    private Vector3 getGridPoint(Collider spawn)
    {
        float spawnHeight = spawn.bounds.max.x - spawn.bounds.min.x;
        float spawnWidth = spawn.bounds.max.z - spawn.bounds.min.z;
        float agentSize = this.GetComponentInChildren<CapsuleCollider>().bounds.size.x + 0.1f;
        int agentsCount = this.manager.numOfAgents;
        int gridSize = (int)Math.Sqrt(Math.Ceiling(agentsCount / agentSize));
        float heightStep = spawnHeight / gridSize;
        float widthStep = spawnWidth / gridSize;

        List<Vector3> list = new List<Vector3>();

        for (int i = 1; i <= gridSize; i++)
            for (int j = 1; j <= gridSize; j++)
            {
                Vector3 point = new Vector3(spawn.bounds.min.x + (heightStep * i), 0f, spawn.bounds.min.z + (widthStep * j));
                list.Add(point);
            }

        int index = (int)(normalizeInRange(this.agentID, 0, agentsCount) * (list.Count-1));
        return list[index];
    }

    private Vector3[] getCircularPoints()
    {
        float angleStep = 360f / this.manager.numOfAgents;
        Vector3[] retPoints = new Vector3[2];

        retPoints[0].x = this.manager.circularSpawnRadius * (float) Mathf.Sin(this.agentID * angleStep * Mathf.Deg2Rad);
        retPoints[0].y = 0;
        retPoints[0].z = this.manager.circularSpawnRadius * (float) Mathf.Cos(this.agentID * angleStep * Mathf.Deg2Rad);

        retPoints[1] = -1f * retPoints[0];
        return retPoints;
    }

    static int SortGoalsByName(Monitor_Training.GoalAndSpawn g1, Monitor_Training.GoalAndSpawn g2)
    {
        return g1.goalCollider.gameObject.name.CompareTo(g2.goalCollider.gameObject.name);
    }

    //Generate two random points from two different GoalSpawnAreas.
    //One goal point and one spawn point
    private Vector3[] randomSpawnPoints()
    {
        Vector3 goalPointRet;
        Vector3 spawnPointRet = Vector3.zero;
        if (this.manager.circularSpawn == true)
        {
            Vector3[] circleRetPoints = getCircularPoints();
            spawnPointRet = circleRetPoints[0];
            goalPointRet = circleRetPoints[1];
        }
        else {
            this.goals.Sort(SortGoalsByName);
            int randomSpawnIndex;
            Collider tempArea;
            if (this.manager.spawnEqually)
            {
                //Select a goal area and a random goal point in that area
                randomSpawnIndex = this.agentID % this.goals.Count;
                tempArea = goals[randomSpawnIndex].goalCollider;
                goalPointRet = new Vector3(UnityEngine.Random.Range(tempArea.bounds.min.x, tempArea.bounds.max.x), 0f, UnityEngine.Random.Range(tempArea.bounds.min.z, tempArea.bounds.max.z));
            }
            else
            {
                //Select a goal area and a random goal point in that area
                randomSpawnIndex = UnityEngine.Random.Range(0, this.goals.Count);
                tempArea = goals[randomSpawnIndex].goalCollider;
                goalPointRet = new Vector3(UnityEngine.Random.Range(tempArea.bounds.min.x, tempArea.bounds.max.x), 0f, UnityEngine.Random.Range(tempArea.bounds.min.z, tempArea.bounds.max.z));
            }
            //Remove selected goal area so will not select is as goal area too
            Monitor_Training.GoalAndSpawn tempBeforeRemove = this.goals[randomSpawnIndex];
            this.goals.Remove(this.goals[randomSpawnIndex]);
            Collider tempArea2 = null;
            if (this.manager.oppositeGoal == true || this.localOppositeGoal == true)
            {
                bool flag = false;
                while (!flag)
                {
                    randomSpawnIndex = UnityEngine.Random.Range(0, this.goals.Count);
                    tempArea2 = this.goals[randomSpawnIndex].goalCollider;
                    if ((tempArea.transform.position.x == 0 && tempArea2.transform.position.x == 0) || (tempArea.transform.position.z == 0 && tempArea2.transform.position.z == 0))
                        flag = true;
                }
            }
            else {
                randomSpawnIndex = UnityEngine.Random.Range(0, this.goals.Count);
                tempArea2 = this.goals[randomSpawnIndex].goalCollider;
            }

            if (this.manager.gridSpawn == true)
                spawnPointRet = getGridPoint(tempArea2);
            else
                spawnPointRet = new Vector3(UnityEngine.Random.Range(tempArea2.bounds.min.x, tempArea2.bounds.max.x), 0f, UnityEngine.Random.Range(tempArea2.bounds.min.z, tempArea2.bounds.max.z));

            this.goals.Add(tempBeforeRemove);
        }
        
        Vector3[] ret = new Vector3[2];
        ret[0] = spawnPointRet;
        ret[1] = goalPointRet;
        return ret;
    }

    //Observations that the agent receives at every step
    public override void CollectObservations(VectorSensor sensor)
    {
        //Movement
        var localVelocity = transform.InverseTransformDirection(this.AgentRb.velocity);
        sensor.AddObservation(localVelocity.x);// 1
        sensor.AddObservation(localVelocity.z);// 1

        //Goal
        float normalized_goalDistance = normalizeInRange(this.currentGoalDistance, 0, this.manager.maxDistance);
        sensor.AddObservation(normalized_goalDistance); // 1
        sensor.AddObservation(this.currentAngle); // 1

        //Weights
        float normalized_collision = normalizeInRange(this.collWeight, this.manager.collMin, this.manager.collMax);
        float normalized_goal = normalizeInRange(this.goalWeight, this.manager.goalMin, this.manager.goalMax);
        float normalized_group = normalizeInRange(this.groupWeight, this.manager.groupMin, this.manager.groupMax);
        float normalized_interact = normalizeInRange(this.interWeight, this.manager.interMin, this.manager.interMax);

        sensor.AddObservation(normalized_collision); // 1
        sensor.AddObservation(normalized_goal); // 1
        sensor.AddObservation(normalized_group); // 1
        sensor.AddObservation(normalized_interact); // 1
    }

    //Run every time agent  receives a new action from the action space
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        //Save path to export csv later
        if (this.manager.saveRoutes)
            appendToRoutes();

        //Assign appropriate rewards based on last action taken.
        assignRewards();

        //Visualize lines for debugging
        Vector3 forwardResized = transform.forward * 1f;
        Debug.DrawRay(transform.position, forwardResized, Color.white);

        float g = forwardResized.magnitude / this.goalVector.magnitude;
        Debug.DrawRay(transform.position, this.goalVector * g, Color.green);
    }

    //Select a random action from the four below to help agent unstuck
    private Vector3 unstuckAction()
    {
        int randomAction = UnityEngine.Random.Range(0, 4);
        switch (randomAction)
        {
            case 0:
                return transform.forward * 0.05f;
            case 1:
                return transform.forward * -0.025f;
            case 2:
                return transform.right * -0.025f;
            case 3:
                return transform.right * 0.025f;
        }
        return Vector3.zero;
    }

    //Move the agent based on the action taken
    private void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];

        //If agent stays still 20 consecutive times, select a random action
        if (this.stillCounter >= 20)
        {
            dirToGo = unstuckAction();
            this.stillCounter = 0;
        }

        if (this.rotationCounter >= 20)
        {
            this.rotationCounter = 0;
        }

        switch (action)
        {
            case 0:
                this.stillCounter++;
                //If agent stay still for a while, select a random action ton unblock
                break;
            case 1:
                this.stillCounter = 0;
                this.rotationCounter = 0;
                dirToGo = transform.forward * 0.75f;
                break;
            case 2:
                this.stillCounter = 0;
                this.rotationCounter = 0;
                dirToGo = transform.forward * -0.1f;
                break;
            case 3:
                this.rotationCounter++;
                rotateDir = transform.up * 1f;
                break;
            case 4:
                this.rotationCounter++;
                rotateDir = transform.up * -1f;
                break;
            case 5:
                this.stillCounter = 0;
                this.rotationCounter = 0;
                dirToGo = transform.right * -0.1f;
                break;
            case 6:
                this.stillCounter = 0;
                this.rotationCounter = 0;
                dirToGo = transform.right * 0.1f;
                break;
        }
        transform.Rotate(rotateDir, Time.fixedDeltaTime * this.turnSpeed);
        AgentRb.AddForce(dirToGo * this.moveSpeed, ForceMode.VelocityChange);
    }

    //Move agent using keyboard just for testing
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }

    //Assign rewards to the agent 
    private void assignRewards()
    {
        this.goalVector = this.goalPos - transform.position;

        this.currentAngle = Vector3.Angle(transform.forward, goalVector);
        this.currentGoalDistance = Vector3.Distance(transform.position, this.goalPos);

        //Goal Arrival Reward
        if (this.currentGoalDistance <= this.manager.goalDistanceThreshold)
        {
            AddReward(+1f * this.goalWeight);
            Debug.Log("Goal");
            EpisodeEnded();
        }

        //Moving towards goal reward
        if ((this.currentAngle <= 45f) && (this.currentGoalDistance < this.goalDistance))
        {
            AddReward(+0.00075f * this.goalWeight);
            this.goalDistance = this.currentGoalDistance;
        }
        else
            AddReward(-0.00025f * this.goalWeight);

        //Group reward
        getClosestAgent();
        float distanceToClosestAgent = this.manager.groupDistanceThreshold;
        if (this.closestAgent != null)
            distanceToClosestAgent = Vector3.Distance(transform.position, this.closestAgent.transform.position);
        float dot_group = Vector3.Dot(transform.forward, (this.groupCenterPoint - transform.position).normalized);
        //if is agent is near to closed agent and looking towards the center of mass among close agents and there are less than maxNeighbours in a radius, give reward
        if (distanceToClosestAgent <= this.manager.groupDistanceThreshold && dot_group >= 0.7f && this.closeAgents <= this.manager.maxNeighbours)
            AddReward(+0.001f * this.groupWeight);

        //Interacting with objects reward
        getClosestInteraction();
        float distanceToClosestInteraction = this.manager.interactionDistanceThreshold;
        if (this.closestInteraction != null)
        {
            Vector3 interactionPoint = new Vector3(this.closestInteraction.transform.position.x, 0, this.closestInteraction.transform.position.z);
            distanceToClosestInteraction = Vector3.Distance(transform.position, interactionPoint);
        }
        float dot_interact = Vector3.Dot(transform.forward, (this.closestInteraction.transform.position - transform.position).normalized);
        //if is agent is near to closed interaction object and looking towards it and there are less than maxNeighbours in a radius, give reward
        if (distanceToClosestInteraction <= this.manager.interactionDistanceThreshold && dot_interact >= 0.225f && this.closeAgents <= this.manager.maxNeighbours)
            AddReward(+0.001f * this.interWeight);

        //Add a negative reward to each step to make agent find its goal as fast as possible
        AddReward(-0.00015f * this.goalWeight);
    }
    
    private void EpisodeEnded()
    {
        this.countEpisode++;
        if (this.manager.oneEpisodeOnly)
        {
            if (this.manager.saveRoutes)
            {
                string name = this.agentID + "_" + this.countEpisode;
                this.manager.saveRoute(this.startingPos, this.goalPos, this.collisionsCount, name, GetCumulativeReward(), this.route);
            }
            Destroy(this.gameObject);
            return;
        }
        if (this.manager.saveRoutes && this.manager.stopSaving)
        {
            string name = this.agentID + "_" + this.countEpisode;
            this.manager.saveRoute(this.startingPos, this.goalPos, this.collisionsCount, name, GetCumulativeReward(), this.route);
        }
        EndEpisode();
    }

    public float normalizeInRange(float value, float min, float max)
    {
        float scaledValue = (value - min) / (max - min);
        return scaledValue;
    }

    private Vector3 calculateCenter(List<Vector3> points)
    {
        Vector3 center = new Vector3(0, 0, 0);
        float count = 0;
        foreach (Vector3 p in points)
        {
            center += p;
            count++;
        }
        return center / count;
    }

    //Get the GameObject of closest agent
    private void getClosestAgent()
    {
        List<Vector3> neighboursPoints = new List<Vector3>();
        this.closeAgents = -1;
        this.agents.Clear();


        GameObject agentMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (Transform child in this.agentParent)
        {
            this.agents.Add(child.gameObject);
            float dist = Vector3.Distance(child.position, transform.position);
            if (dist <= this.manager.maxNeighbours)
            {
                this.closeAgents++;
                neighboursPoints.Add(child.position);
            }
            
            if (dist < minDist && dist > 0.01f)
            {
                agentMin = child.gameObject;
                minDist = dist;
            }
        }

        if (neighboursPoints.Count > 1)
            this.groupCenterPoint = calculateCenter(neighboursPoints);
        else if(neighboursPoints.Count == 1)
            this.groupCenterPoint = agentMin.transform.position;
        else
            this.groupCenterPoint = transform.position;

        this.closestAgent = agentMin;
        this.closestAgentID = agentMin.GetComponent<Agent_Training>().agentID;
    }

    //Get the GameObject of closest interaction object
    private void getClosestInteraction()
    {
        this.interactionObjects.Clear();
        this.interactionObjects.AddRange(GameObject.FindGameObjectsWithTag("Interaction"));

        GameObject objectMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (GameObject t in this.interactionObjects)
        {
            float dist = Vector3.Distance(t.transform.position, currentPos);
            if (dist < minDist)
            {
                objectMin = t;
                minDist = dist;
            }
        }
        this.closestInteraction = objectMin;
    }

    //Runs when colliders get triggered
    private void OnTriggerEnter(Collider other)
    {
        //Agents collide to each other
        if (other.tag == "AgentCollider" && StepCount >= 100f)
        {
            AddReward(-0.01f * this.collWeight);
            float collDistance = Vector3.Distance(transform.position, other.gameObject.transform.position);
            float agentCollRadius = this.transform.Find("Colliders").transform.Find("BodyCollider").GetComponent<CapsuleCollider>().radius * this.gameObject.transform.localScale.x;
            if (collDistance < (1.9f * agentCollRadius))
            {
                Debug.Log("Agent Collision");
                this.collisionsCount++;
            }
        }
        //Agent collide to an obstacle
        if (other.tag == "Obstacle" || other.tag == "Interaction")
        {
            Debug.Log("Obstacle Collision");
            AddReward(-0.5f * this.collWeight);
            this.collisionsCount++;
            if (this.gameObject.name.Contains("Demo") == false)
                EpisodeEnded();
        }
    }

    IEnumerator changeWeights()
    {
        yield return new WaitForSeconds(this.manager.timeToInheritWeights);
        this.inWeightRegion = true;
    }

    private void appendToRoutes()
    {
        float time = Time.fixedTime;
        //Not record the first step until everything is initialized
        if (time > 0)
        {
            float x = transform.position.x;
            float z = transform.position.z;
            float look_y = transform.localEulerAngles.y;

            //Weights
            float normalized_goal = normalizeInRange(this.goalWeight, this.manager.goalMin, this.manager.goalMax);
            float normalized_collision = normalizeInRange(this.collWeight, this.manager.collMin, this.manager.collMax);
            float normalized_group = normalizeInRange(this.groupWeight, this.manager.groupMin, this.manager.groupMax);
            float normalized_interact = normalizeInRange(this.interWeight, this.manager.interMin, this.manager.interMax);

            float[] tempArr = new float[] { this.manager.frameCount, time, x, z, look_y, normalized_goal, normalized_collision, normalized_interact, normalized_group };
            this.route.Add(tempArr);
        }
    }
}
