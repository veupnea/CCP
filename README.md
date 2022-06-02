## CCP - Configurable Crowd Profiles [SIGGRAPH 2022]
<a href="https://orcid.org/0000-0002-7614-1059" target="_blank">Andreas Panayiotou</a>, <a href="https://orcid.org/0000-0003-0699-2445" target="_blank">Theodoros Kyriakou</a>, <a href="https://orcid.org/0000-0001-9854-3369" target="_blank">Marilena Lemonari</a>, <a href="http://www.cs.ucy.ac.cy/~yiorgos/" target="_blank">Yiorgos
Chrysanthou</a> ,and <a href="https://totis77.github.io/" target="_blank">Panayiotis Charalambous</a>

![Demo Image](https://github.com/veupnea/CCP/blob/main/Images/demo.png)

<p align="justify">
Diversity among agents' behaviors and heterogeneity in virtual crowds in general, is an important aspect of crowd simulation as it is crucial to the perceived realism and plausibility of the resulting simulations. Heterogeneous crowds constitute the pillar in creating numerous real-life scenarios such as museum exhibitions, which require variety in agent behaviors, from basic collision avoidance to more complex interactions both among agents and with environmental features. Most of the existing systems optimize for specific behaviors such as goal seeking, and neglect to take into account other behaviors and how these interact together to form diverse agent profiles. In this paper, we present a RL-based framework for learning multiple agent behaviors concurrently. We optimize the agent by varying the importance of the selected behaviors (goal seeking, collision avoidance, interaction with environment, and grouping) while training; essentially we have a reward function that changes dynamically during training. The importance of each separate sub-behavior is added as input to the policy, resulting in the development of a single model capable of capturing as well as enabling dynamic run-time manipulation of agent profiles; thus allowing configurable profiles. Through a series of experiments, we verify that our system provides users with the ability to design virtual scenes; control and mix agent behaviors thus creating personality profiles, and assign different profiles to groups of agents. Moreover, we demonstrate that interestingly the proposed model generalizes to situations not seen in the training data such as a) crowds with higher density, b) behavior weights that are outside the training intervals and c) to scenes with more intricate environment layouts.
</p>

<br>

<p align="center">
	- <a href="https://www.youtube.com/watch?v=k5SAOnisBas" target="_blank">Video</a> | <a href="https://github.com/veupnea/CCP/blob/main/PDF%20Files/CCP_Configurable_Crowd_Profiles.pdf" target="_blank">Paper</a> | <a href="https://github.com/veupnea/CCP/blob/main/PDF%20Files/Documentation.pdf" target="_blank">Documentation</a> - </p>
</p>

<br>

<p align="center" dir="auto">
	<a href="https://www.youtube.com/watch?v=k5SAOnisBas" rel="nofollow">
		<img align="center" width="400px" src="https://github.com/veupnea/CCP/blob/main/Images/youtube_image.png"/>
	</a>
</p>






