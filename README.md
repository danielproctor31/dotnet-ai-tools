# dotnet-ai-tools

## Overview
This repository contains a examples of Microsoft.Extensions.AI, OpenAI and ModelContextProtocol that can be used for creating AI tools.

## Dotnet.AI.Console
Simple console application to interact with an LLM utilizing tools via both local functions and remote functions via an MCP Server.

## Dotnet.AI.MCP.Server

A Model Context Protocol (MCP) server that can be used to expose functions as remotely for use in AI applications.

### Registering the MCP Server in VSCode
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