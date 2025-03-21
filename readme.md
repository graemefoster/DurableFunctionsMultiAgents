# Durable Agents

## What is this?

Everyone is talking about Multi-Agent architectures. People are also starting to talk about scaling these out - similar to the micro-service distributed architecture conversations of many years ago.

This accelerator aims to do 2 things:

 - Shine a light onto the insides of a multi-agent architecture. Show the internal prompts used, and the chit-chat between the agents
 - Run a user's interaction with the agent network using Durable Functions to remove the synchronous coupling often seen between agents in most multi-agent samples.

 We've produced a fairly simple Canvas style application that helps a user to write a story. It's a class of multi-agent application that we've seen in the wild, and presents a use-case that's nice to reason with.

 One thing we added was a ```DIFFER``` agent. Lots of the Canvas applications are one-way. Change the prompts or the input data, and the Agents re-write the entire content. We wanted to experiement with a bi-directional flow. The ```DIFFER``` agent lets us edit the generated content directly, and incorporate the intent back into the content.

 ## SDKs and tools used

 We've purposefully stayed away from using the current batch of multi-agent enabling frameworks, e.g. Autogen, Semantic Kernel, LangGraph, etc. The main reason is to make it easy to open-the-box and show what's going on inside. We've decided to build simple multi-agent orchestration from scratch to try and demonstrate key points in the architecture.

 What we've landed on are:

  - **A web-client** written in React, using SignalR to facilitate an asynchronous bidirectional conversation between user and the agents
  - **Durable Functions** running on the Azure Functions host. We use Durable Entities to represent the individual entities. This allows us to scale to massive number of users, it simplifies state management, and provides a natural serverless programming model.
  - **Custom Orchestration logic** so we can shine the light inside the closed room where the agents work together!

We've built a handful of agents to demonstrate a particular style of conversation (a constrained network where each agent is pre-instructed who it can talk to).

  > A key decisions in any multi-agent setup is the conversation style. Just like people, different use-cases need different patterns of conversation. 

## More technical details

### Human in the Loop

Having a human in the loop gave us a requirement to hand-off a thread back to the human when required. To facilitate this we leverage a Durable Orchestrations ability to ```WaitForExternalEvent```. This lets us yield an orchestration, use SignalR to ask the user to respond, and then to pickup the orchestration when it's time.

### Tools to augment conversations

Taking control of the agents allowed us to experiment with bending the agents to do what we wanted... For example, in the context of writing a story we didn't want every old draft of the story to clutter the conversation history.

Using tools allows us to ask a ```WRITER``` agent to do 2 things. 
 1. ```Publish``` a new story using a pre-defined tool
 2. ```SendMessage``` to the next agent (the ```IMPROVER```) to think of follow up questions for the ```HUMAN```.

Having low level control over the agents and orchestrations make this kind of behaviour simple to model.
