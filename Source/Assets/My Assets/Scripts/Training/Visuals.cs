using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class Visuals : MonoBehaviour
{
    Color bodyColor;
    TrailRenderer trail;
    MeshRenderer body;
    Monitor_Training manager;
    Agent_Training agent;

    // Start is called before the first frame update
    void Start()
    {
        this.manager = GameObject.Find("Environment").GetComponent<Monitor_Training>();
        this.agent = this.gameObject.GetComponent<Agent_Training>();
        this.trail = transform.GetComponent<TrailRenderer>();
        this.body = transform.GetChild(0).GetComponent<MeshRenderer>();
        this.bodyColor = this.body.material.color;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.manager.coloredWeights)
        {
            float normalized_collision = this.agent.normalizeInRange(this.agent.collWeight, this.manager.collMin, this.manager.collMax);
            float normalized_goal = this.agent.normalizeInRange(this.agent.goalWeight, this.manager.goalMin, this.manager.goalMax);
            float normalized_group = this.agent.normalizeInRange(this.agent.groupWeight, this.manager.groupMin, this.manager.groupMax);
            float normalized_interact = this.agent.normalizeInRange(this.agent.interWeight, this.manager.interMin, this.manager.interMax);
            Color color = new Color(normalized_goal, normalized_interact, normalized_group, Mathf.Clamp(normalized_collision * 0.5f + 0.5f, 0.5f, 1f));
            this.body.material.color = color;
            this.trail.startColor = color;
            this.trail.endColor = color;
        }
        else
        {
            this.body.material.color = this.bodyColor;
            this.trail.startColor = this.bodyColor;
            this.trail.endColor = this.bodyColor;
        }
    }
}
