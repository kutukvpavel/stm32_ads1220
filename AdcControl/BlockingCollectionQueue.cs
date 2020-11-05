using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AdcControl
{
    public class BlockingCollectionQueue
    {
        private BlockingCollection<Action> _jobs = new BlockingCollection<Action>();

        public BlockingCollectionQueue()
        {
            var thread = new Thread(new ThreadStart(OnStart));
            thread.IsBackground = true;
            thread.Start();
        }

        public void Enqueue(Action job)
        {
            _jobs.Add(job);
        }

        private void OnStart()
        {
            foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            {
                job();
            }
        }
    }
}
