using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sprinklers_Connectors.Models
{
    public enum ConnectionFailureReason
    {
        None,

        // No fire protection pipe in the model (or all are excluded)
        NoPipeFound,

        // The sprinkler itself has no free connector (all connectors are connected)
        NoUnconnectedSprinklerConnector,

        // Project() on the pipe failed (rare - strange geometry)
        ProjectionFailed,

        // Pipe.Create failed (e.g., missing pipe type/system/level, or zero length)
        BranchPipeCreationFailed,

        // PlumbingUtils.BreakCurve failed (break point too close to an end)
        MainPipeBreakFailed,

        // NewTeeFitting failed (connectors incompatible / odd alignment)
        TeeCreationFailed,

        // Both NewElbowFitting and NewUnionFitting failed
        ElbowCreationFailed,

        // Everything succeeded up to connecting the sprinkler itself to the branch
        FinalConnectionFailed,

        // Any unexpected exception
        UnexpectedException
    }

    public class SprinklerConnectionResult
    {
        public bool Success { get; private init; }

        public ConnectionFailureReason Reason { get; private init; } =
            ConnectionFailureReason.None;

        public static SprinklerConnectionResult Ok() =>
            new() { Success = true };

        public static SprinklerConnectionResult Fail(
            ConnectionFailureReason reason) =>
            new() { Success = false, Reason = reason };
    }
}
