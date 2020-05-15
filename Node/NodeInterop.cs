using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.NodeServices;

namespace Sociosearch.NET.Node
{
    public class NodeInterop
    {
        private readonly INodeServices _node;

        public NodeInterop(INodeServices nodeServices)
        {
            _node = nodeServices;
        }

        // Can also pass parameters to the Node scripts if needed
        public string TestNodeInterop()
        {
            return TestNodeInteropHelper(_node).Result;
        }

        public static async Task<string> TestNodeInteropHelper([FromServices] INodeServices nodeServices)
        {
            var result = await nodeServices.InvokeAsync<int>("Node/addNumbers", 1, 2);
            return "1 + 2 = " + result;
        }
    }
}
