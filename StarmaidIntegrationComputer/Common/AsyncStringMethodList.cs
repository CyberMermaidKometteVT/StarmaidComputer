using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarmaidIntegrationComputer.Common
{
    //Worried about race conditions and stuff in this, especially on the form; maybe I shouldn't make it async?
    internal class AsyncStringMethodList: List<Func<string, Task>>
    {
        public Task Execute(string message)
        {
            var runningTasks = this.Select(func => func(message)).ToArray();

            return Task.WhenAll(runningTasks);
        }
    }
}
