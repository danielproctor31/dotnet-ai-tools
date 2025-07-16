# dotnet-ai-tools

## Overview
This repository contains a examples of Microsoft.Extensions.AI, OpenAI and ModelContextProtocol that can be used for creating AI tools.

## Dotnet.AI.Console
Simple console application to interact with an LLM utilizing tool calls in your code.

## Dotnet.AI.MCP.Server

https://code.visualstudio.com/docs/copilot/chat/mcp-servers#_add-an-mcp-server

VSCode configuration. Replace the path with the full path to the project file in your local environment.
```json
{
    "inputs": [],
    "servers": {
        "MyMCPServer": {
            "type": "stdio",
            "command": "dotnet",
            "args": [
                "run",
                "--project",
                "\\src\\Dotnet.AI.MCP.Server\\Dotnet.AI.MCP.Server.csproj"
            ]
        }
    }
}
```