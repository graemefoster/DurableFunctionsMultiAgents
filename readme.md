# Durable Agents

## What is this?

Everyone is talking about Multi-Agent architectures. People are also starting to talk about scaling these out - similar to the micro-service distributed architecture conversations of many years ago.

This accelerator aims to do 2 things:

 - Shine a light onto the insides of a multi-agent architecture. Show the internal prompts used, and the chit-chat between the agents
 - Run a user's interaction with the agent network using Durable Functions to remove the synchronous coupling often seen between agents in most multi-agent samples.

 ## SDKs and tools used

 We've purposefully stayed away from using the current batch of multi-agent enabling frameworks, e.g. Autogen, Semantic Kernel, LangGraph, etc. The main reason is to make it easy to open-the-box and show what's going on inside. We've decided to build simple multi-agent orchestration from scratch to try and demonstrate key points in the architecture.

 What we've landed on are:

  - **A web-client** written in React, using SignalR to facilitate an asynchronous bidirectional conversation between user and the agents
  - **Durable Functions** running on the Azure Functions host. We use Durable Entities to represent the individual entities. This allows us to scale to massive number of users, it simplifies state management, and provides a natural serverless programming model.

We've built a handful of agents to demonstrate a particular style of conversation (a constrained network where each agent is pre-instructed who it can talk to).

  > A key decisions in any multi-agent setup is the conversation style. Just like people, different use-cases need different patterns of conversation. 


