using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AudioPass
{
    public class Settings
    {
        public static IObservable<dynamic> Observe(string fileName)
        {
            return Observable
                .Interval(TimeSpan.FromSeconds(1)).Merge(Observable.Return(0L))
                .Select(_ => TryGetWriteTime(fileName))
                .DistinctUntilChanged()
                .Where(fileTime => fileTime != null)
                .Select(_ => TryParseJson(fileName))
                .Where(jobject => jobject != null)
                .Cast<dynamic>()
                .Replay(1).RefCount();
        }

        private static JObject TryParseJson(string fileName)
        {
            try
            {
                return JObject.Parse(File.ReadAllText(fileName));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static DateTime? TryGetWriteTime(string fileName)
        {
            try
            {
                return File.GetLastWriteTimeUtc(fileName);
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
