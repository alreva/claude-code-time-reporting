#!/usr/bin/env node

// Simple WebSocket MCP client test
// Tests the end-to-end flow: WebSocket -> MCP Server -> GraphQL API -> Database

const WebSocket = require('ws');

const ws = new WebSocket('ws://localhost:5002/mcp');

let messageId = 1;

function sendMessage(method, params = []) {
    const message = {
        jsonrpc: '2.0',
        id: messageId++,
        method,
        params: Array.isArray(params) ? params : [params]
    };
    console.log('\n→ Sending:', JSON.stringify(message, null, 2));
    ws.send(JSON.stringify(message));
}

ws.on('open', () => {
    console.log('✅ WebSocket connected to ws://localhost:5002/mcp');

    // Step 1: Initialize MCP session
    // StreamJsonRpc expects positional params array
    const initParams = {
        protocolVersion: '2024-11-05',
        capabilities: {},
        clientInfo: {
            name: 'test-client',
            version: '1.0.0'
        }
    };

    const message = {
        jsonrpc: '2.0',
        id: messageId++,
        method: 'initialize',
        params: [initParams]  // Send as positional array
    };
    console.log('\n→ Sending:', JSON.stringify(message, null, 2));
    ws.send(JSON.stringify(message));
    return; // Don't call sendMessage
});

ws.on('message', (data) => {
    const message = JSON.parse(data.toString());
    console.log('\n← Received:', JSON.stringify(message, null, 2));

    // After initialize, list tools
    if (message.result && message.result.serverInfo) {
        console.log('\n✅ MCP server initialized:', message.result.serverInfo.name);
        sendMessage('tools/list', []);  // No parameters
    }

    // After tools/list, call get_available_projects
    if (message.result && message.result.tools) {
        console.log(`\n✅ Found ${message.result.tools.length} tools`);
        message.result.tools.forEach(tool => {
            console.log(`  - ${tool.name}: ${tool.description}`);
        });

        console.log('\n🧪 Testing get_available_projects tool...');
        sendMessage('tools/call', [{
            name: 'get_available_projects',
            arguments: {
                activeOnly: true
            }
        }]);
    }

    // Result from tool call
    if (message.result && message.result.content) {
        console.log('\n✅ Tool execution result:');
        message.result.content.forEach(item => {
            if (item.type === 'text') {
                console.log(item.text);
            }
        });

        if (!message.result.isError) {
            console.log('\n🎉 End-to-end test PASSED!');
            console.log('✅ WebSocket MCP Server → GraphQL API → Database flow verified');
            ws.close();
            process.exit(0);
        } else {
            console.error('\n❌ Tool execution failed');
            ws.close();
            process.exit(1);
        }
    }
});

ws.on('error', (error) => {
    console.error('❌ WebSocket error:', error.message);
    process.exit(1);
});

ws.on('close', () => {
    console.log('\n👋 WebSocket connection closed');
});

// Timeout after 10 seconds
setTimeout(() => {
    console.error('\n❌ Test timeout');
    ws.close();
    process.exit(1);
}, 10000);
