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

        public string GetZacksRank(string symbol)
        {
            return MyAction(_node).Result;
        }

        public static async Task<string> MyAction([FromServices] INodeServices nodeServices)
        {
            var result = await nodeServices.InvokeAsync<int>("Node/addNumbers", 1, 2);
            return "1 + 2 = " + result;
        }
    }
}
