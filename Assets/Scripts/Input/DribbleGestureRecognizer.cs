using System.Collections.Generic;
using UnityEngine;

namespace GEL
{
    public enum DribbleStyle
    {
        Simple,
        Feint,
        Roulette,
        DoubleTouch,
        StepOver,
        Elastico
    }

    public static class DribbleGestureRecognizer
    {
        // The only two gameplay outcomes are:
        // - straight / near-straight path -> normal burst
        // - anything with a real bend          -> Elastico flair
        public const float StraightDeviationRatio = 0.075f;
        public const float StraightPathRatio = 1.12f;

        public static DribbleStyle Recognize(
            List<Vector2> points,
            out float confidence,
            out float feintAngle,
            out float cumulativeTurn)
        {
            confidence = 0f;
            feintAngle = 0f;
            cumulativeTurn = 0f;

            if (points == null || points.Count < 3)
                return DribbleStyle.Simple;

            Vector2 start = points[0];
            Vector2 end = points[points.Count - 1];
            Vector2 net = end - start;
            float pathLength = 0f;

            for (int i = 1; i < points.Count; i++)
                pathLength += Vector2.Distance(points[i - 1], points[i]);

            if (pathLength < 1f)
                return DribbleStyle.Simple;

            // Distance from the straight start->end chord.
            float maxDeviation = 0f;
            if (net.sqrMagnitude > 0.0001f)
            {
                Vector2 direction = net.normalized;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);

                for (int i = 0; i < points.Count; i++)
                {
                    float signedDistance = Vector2.Dot(points[i] - start, perpendicular);
                    maxDeviation = Mathf.Max(maxDeviation, Mathf.Abs(signedDistance));
                }
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                    maxDeviation = Mathf.Max(
                        maxDeviation, Vector2.Distance(points[i], start));
            }

            float deviationRatio = maxDeviation / Mathf.Max(1f, pathLength);
            float pathRatio = pathLength / Mathf.Max(1f, net.magnitude);

            bool straight = deviationRatio <= StraightDeviationRatio &&
                            pathRatio <= StraightPathRatio;

            if (straight)
            {
                confidence = 0.9f;
                return DribbleStyle.Simple;
            }

            confidence = 0.86f;
            return DribbleStyle.Elastico;
        }
    }
}
