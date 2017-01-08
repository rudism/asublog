namespace Asublog
{
    using Core;
    using Mono.Unix;
    using Mono.Unix.Native;

    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new Asublog();
            app.Run();

            app.Log.Info("Asublog is running");

            var signals = new[]
            {
                new UnixSignal(Signum.SIGINT),
                new UnixSignal(Signum.SIGTERM)
            };

            for(bool exit = false; !exit;)
            {
                int id = UnixSignal.WaitAny(signals);
                if(id >= 0 && id < signals.Length)
                    if(signals[id].IsSet) exit = true;
            }

            app.Log.Info("Interrupt received, exiting");
            app.Dispose();
        }
    }
}
