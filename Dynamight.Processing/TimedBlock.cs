using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Processing
{
    public struct TimedBlockRecord
    {
        public TimeSpan Span;
        public string Id;

        public override string ToString()
        {
            return string.Format("{0} : {1}", Id, Span);
        }
    }

    public class TimedBlockRecorder
    {
        Queue<TimedBlockRecord> queue = new Queue<TimedBlockRecord>();

        public Action<TimeSpan> BuildRecorder(string id)
        {
            return (ts) =>
            {
                queue.Enqueue(new TimedBlockRecord() { Id = id, Span = ts });
            };
        }

        public TimedBlock GetBlock(string id)
        {
            return new TimedBlock(BuildRecorder(id));
        }

        public override string ToString()
        {
            var all = queue.GroupBy(q => q.Id).Select(gr => new TimedBlockRecord() { Id = gr.Key, Span = new TimeSpan(gr.Reverse().Take(30).Select(g => g.Span.Ticks).Sum() / gr.Reverse().Take(30).Count()) });
            return string.Join("\n", all.Select(tbr => tbr.ToString()));
        }

        public string AverageAll()
        {
            var all = queue.GroupBy(q => q.Id).Select(gr => new TimedBlockRecord() { Id = gr.Key, Span = new TimeSpan(gr.Reverse().Take(30).Select(g => g.Span.Ticks).Sum() / gr.Reverse().Take(30).Count()) });
            var tbr = new TimedBlockRecord()
            {
                Id = "Average all",
                Span = new TimeSpan(all.Select(q => q.Span).Sum(q => q.Ticks))
            };
            return tbr.ToString();
        }
    }

    public class TimedBlock : IDisposable
    {
        Stopwatch watch;
        Action<TimeSpan> onFinish;

        public TimedBlock(Action<TimeSpan> onFinish)
        {
            watch = new Stopwatch();
            watch.Start();
            this.onFinish = onFinish;
        }

        public void Dispose()
        {
            watch.Stop();
            onFinish(watch.Elapsed);
        }
    }
}

