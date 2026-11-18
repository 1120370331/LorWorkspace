using System;
using System.Reflection;
using System.Threading;

internal static class VerifyMusicDamagePlaybackContext
{
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: verify_music_damage_playback_context <Steria.dll>");
            return 2;
        }

        Assembly steria = Assembly.LoadFrom(args[0]);
        Type context = steria.GetType("Steria.MusicDamagePlaybackContext", true);
        MethodInfo enter = context.GetMethod("Enter", BindingFlags.Public | BindingFlags.Static);
        MethodInfo matches = context.GetMethod("Matches", BindingFlags.Public | BindingFlags.Static);
        PropertyInfo isActive = context.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Static);

        Func<object, object, bool> match = (attacker, target) =>
            (bool)matches.Invoke(null, new[] { attacker, target });
        Func<bool> active = () => (bool)isActive.GetValue(null, null);

        object musicAttacker = new object();
        object musicTarget = new object();
        object otherAttacker = new object();
        object otherTarget = new object();

        Require(!active(), "playback context must start inactive");
        IDisposable scope = (IDisposable)enter.Invoke(
            null,
            new[] { musicAttacker, musicTarget, otherAttacker, otherTarget });
        Require(active(), "scope must activate playback matching");
        Require(match(musicAttacker, musicTarget), "first exact attacker-target pair must match");
        Require(match(otherAttacker, otherTarget), "second exact attacker-target pair must match");
        Require(!match(musicAttacker, otherTarget), "crossed target must not inherit music styling");
        Require(!match(otherAttacker, musicTarget), "crossed attacker must not inherit music styling");
        Require(!match(musicTarget, musicAttacker), "reversed pair must not match");
        Require(!match(null, musicTarget), "null attacker must not match");

        scope.Dispose();
        scope.Dispose();
        Require(!active(), "postfix/finalizer double cleanup must be idempotent");
        Require(!match(musicAttacker, musicTarget), "ordinary hits after cleanup must not match");

        IDisposable exceptionalScope = null;
        try
        {
            exceptionalScope = (IDisposable)enter.Invoke(
                null,
                new[] { musicAttacker, musicTarget, null, null });
            throw new ApplicationException("simulated PrintDamage failure");
        }
        catch (ApplicationException)
        {
        }
        finally
        {
            exceptionalScope?.Dispose();
        }
        Require(!active(), "exception cleanup must not leak into the next hit");

        IDisposable mainThreadScope = (IDisposable)enter.Invoke(
            null,
            new[] { musicAttacker, musicTarget, null, null });
        Exception threadFailure = null;
        Thread thread = new Thread(() =>
        {
            try
            {
                Require(!active(), "playback scope must be thread-local");
                IDisposable localScope = (IDisposable)enter.Invoke(
                    null,
                    new[] { otherAttacker, otherTarget, null, null });
                Require(match(otherAttacker, otherTarget), "thread-local pair must match on its thread");
                localScope.Dispose();
                Require(!active(), "thread-local scope must clean up independently");
            }
            catch (Exception ex)
            {
                threadFailure = ex;
            }
        });
        thread.Start();
        thread.Join();
        Require(threadFailure == null, threadFailure?.ToString() ?? "thread verification failed");
        Require(match(musicAttacker, musicTarget), "other thread cleanup must not clear the main scope");
        mainThreadScope.Dispose();
        Require(!active(), "final main-thread cleanup must restore inactive state");

        Console.WriteLine("PASS: exact-pair playback matching; simultaneous isolation; idempotent and exception cleanup; thread isolation");
        return 0;
    }
}
