<h1 style="text-align:center"> Protein-Protein Interaction Network (PPIN)</h1> <br>
<p style="text-align:center">
<img alt="PPIN" title="PPIN" src="https://imgur.com/wrh6E2L.png">
</p>

<p style="text-align:center">
   3D representation of biological networks using graph theory.
</p>

**[Github Repository](https://github.com/pperuzzo/PPIN)** | [Demo](https://youtu.be/Ib3AAOYMbTI)

## Table of Contents

- [Introduction](#introduction)
- [Problem](#problem)
- [Solution](#solution)
- [Math](#math)
- [How PPIN Works](#how-ppin-works)
- [Future](#future)
- [How to use it](#how-to-use-it)
- [References](#references)

## Introduction
Mathematics describes the world. Thanks to math, we were able to understand many things about our world. On the other side, we still have many unanswered questions. Every day, people all around the world are trying to find meaningful answers to these questions. <br />
Biologists, for instance, question themself daily about fundamental problems of the human race. Why does this certain disease exist? How can we stop it? Can we stop it? How can we prevent it?<br />
And while math may not be able to provide a direct answer to all of these questions, it can for sure provide scientists the tools necessary to answer them. Mathematical tools to help address unanswered questions in biology.<br />
Today, I propose a new improved implementation of an already existing mathematical tool in biology: protein-protein interaction network (PPIN).

## Problem
Whenever it comes to analyzing proteins and their interactions, biologists face many different issues.
After researching the current workflow in the analysis of proteins, I highlighted a few key problems:

- When studying the effect of a drug, it is particularly difficult to understand and visualize how this drug acts on a protein. This is because a single drug can also affect the neighbors of a protein.
- Sometimes, biologists know about the existence of the role of a protein, but they often struggle to find which specific protein is performing that role.
- Sometimes it is known that a certain protein is involved in a disease. Seeing what other proteins might be involved requires a lot of effort.
- Given a signaling pathway, it is difficult to analyze specific steps within that pathway.
- An important part of biology is to teach other people (students or coworkers) about how proteins interact with one another. There is not an easy way to do this today. 

## Solution
PPIN, was born to address all of these problems. PPIN is a fun and easy 3D interactable representation of a network of proteins using Virtual Reality.
The network is fully editable and customizable, through simple hand gestures. The user can select a protein and visualize a network of the closest related proteins. With a simple pinch, the network can be recentered with the selected protein.
Given in input a protein of interest and a know component of the pathway, a path can be visualized between the proteins in the network. 

## Math
As mentioned in the introduction, PPIN is a mathematical tool to be used by biologists. In itself, the core of this tool uses the principles of graph theory.<br />
*Note: the following discussion is implemented in the script `ProteinNetwork.cs`.*<br />
Every protein is a vertex, and every interaction between 2 proteins is an edge.<br />
It is important to note that an edge represents the predicted functional association between two proteins. Therefore, if an edge is represented in the graph, it does not mean that that edge certainly exists.<br />
Every edge has a confidence score that goes from 0 to 1. <br />
The confidence score is calculated based on several factors: 
<ol>
<li>the presence of fusion evidence</li>
<li>neighborhood evidence</li>
<li>cooccurrence evidence</li>
<li>experimental evidence</li>
<li>text mining evidence</li>
<li>database evidence</li>
<li>coexpression evidence</li>
</ol>

The thickness of the edge in PPIN represents its score: the thinner the edge, the lower the score.<br />
For the viewer's sake, PPIN displays only edges that have a confidence score of at least 0.15.<br />

The network is represented internally as a 10x10 matrix. Every row and every column is a protein in the network, starting at 0 with the selected protein. If the ij<sup>th</sup> entry of the matrix is equal to zero, that means that there is no evidence (or not enough evidence) of interaction between the i<sup>th</sup> protein and the j<sup>th</sup> protein. <br />
PPIN provides also a `DebugMatrix.cs` script to visualize the network matrix.<br />

### Network Layout
The `CalculateSpringNetwork(int iterations = 50)` method is where the force-directed drawing algorithm of Fruchterman and Reingold is implemented [[1]](#1). This algorithm uses attractive and repulsive forces between vertices so that there are attractive forces between adjacent vertices and repulsive forces between all pairs of vertices. Moreover, the Fruchterman and Reingold algorithm introduce temperature. The temperature controls the displacement of vertices so that as the layout becomes better, the adjustments become smaller.<br />
This method just computes the ideal position for each vertex. Then, in the `LerpVertices()` method, is where the movement happens. Each vertex is lerped each frame towards its precomputed position. 

### Shortest Path
As previously discussed, sometimes biologists know about a signaling pathway between two proteins in a network. There can be many of these paths, not necessarily only the shortest one.<br />
But it is usually also known to biologists some other components of the pathway. Therefore, we can compute using a modified version of the Breadth-first search algorithm (BFS) [[2]](#2) an actual possible path passing from these 3 known proteins. I choose this algorithm compared to others because the graph is unweighted.<br />
PPIN allows you to use "Path Mode" to visualize a path between:
<ol>
<li>the selected protein (the central protein in the network)</li>
<li>a known component of the pathway</li>
<li>a protein of interest.</li>
</ol>

This process happens in the `GetShortestPath()` method. All the edges in the path change their color in the network. Therefore the user can easily visualize the path.<br />
The modified BFS algorithm stores the predecessor of a given vertex while doing the breadth-first search. To avoid processing a vertex more than once,  `GetShortestPath()` use a boolean visited array to keep track of the already visited vertices.<br />
Finally, the central vertex (ie: the selected protein) gets added to the path. If we are currently visualizing the network for the selected protein, all the other proteins in the network must be connected to that one. So, the shortest path between the selected protein and the known component in the pathway will always exist and always be 1. <br />

## How PPIN Works

<img alt="Architecture" title="Architecture" src="https://imgur.com/fI9WFoI.png" width="750px"> <br />
PPIN was developed using Unity Engine `2019.4.15f1`. The entire codebase is written in C#. All the 3D models inside PPIN were made using Blender.<br />
PPIN, uses Unity XR SDK with the Oculus XR plugin to "communicate" with the Oculus hardware.<br />
In addition, all the data used by PPIN (such as the information about each protein, and all the existing edges between them) are stored in a database using SQLite. SQLite is a good choice for this project as it runs on both desktop, and Android out of the box. Moreover, the data are not sensible, as they are all open source data already.<br />
The last third party used by PPIN is [HandPosing](https://github.com/MephestoKhaan/HandPosing), used to provide the grab interactions in VR.<br />
Through Oculus CLI, a build of PPIN is deployed on Oculus Platform, which is then easily accessible through the Oculus Quest device.<br />
All the code that manage the network is contained in `ProteinNetwork.cs`.


## Future
Some future development for this project include: optimization, making this a tool for education, and add other species.

## How to use it
There are two main options to get started with PPIN. In both cases, you will need an Oculus Quest/Oculus Quest 2. <br />
The first one is to build the project yourself. You can simply clone the GitHub repository and then open the project using Unity `2019.4.15f1` (make sure you have the Android Module installed with your Unity Editor). Then, just switch to Android in the building settings, and finally build on your Quest. This is not recommended, although it will provide more freedom to play around with the source code and the various settings of PPIN. <br />
The other way (the recommended one) is to just sideload the `.apk` build on your Quest. You can use [Side Quest](https://sidequestvr.com/setup-howto) to do this. <br />
**Bonus Option (no Quest needed)**: just sit down and enjoy this [Video](https://youtu.be/Ib3AAOYMbTI) :)

## References
<a id="1">[1]</a>
Roberto Tamassia, *Handbook of Graph Drawing and Visualization* (June 2019), 279-282

<a id="2">[2]</a>
Jeff Erickson, *Algorithms* (June 24, 2013), 385-387
<!--Author's first and last name, *Title of the Work* (place of publication: Publisher Year), page number(s)-->