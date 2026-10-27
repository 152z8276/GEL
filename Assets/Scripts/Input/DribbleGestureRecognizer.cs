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

            if (pathLength < 1f || net.magnitude < 1f)
                return DribbleStyle.Simple;

            float earlyTarget = pathLength * 0.32f;
            float exitTarget = pathLength * 0.65f;
            float accumulated = 0f;
            Vector2 earlyEnd = start;
            Vector2 exitStart = start;

            for (int i = 1; i < points.Count; i++)
            {
                accumulated += Vector2.Distance(points[i - 1], points[i]);
                if (accumulated >= earlyTarget && earlyEnd == start)
                    earlyEnd = points[i];
                if (accumulated >= exitTarget)
                {
                    exitStart = points[i];
                    break;
                }
            }

            Vector2 earlyDirection = earlyEnd - start;
            Vector2 exitDirection = end - exitStart;
            if (earlyDirection.sqrMagnitude > 0.01f && exitDirection.sqrMagnitude > 0.01f)
                feintAngle = Vector2.Angle(earlyDirection, exitDirection);

            int reversals = 0;
            float lastSign = 0f;

            for (int i = 2; i < points.Count; i++)
            {
                Vector2 previous = points[i - 1] - points[i - 2];
                Vector2 current = points[i] - points[i - 1];
                if (previous.sqrMagnitude < 1f || current.sqrMagnitude < 1f) continue;

                cumulativeTurn += Vector2.SignedAngle(previous, current);
                if (Vector2.Dot(previous.normalized, current.normalized) < -0.2f)
                {
                    float sign = Mathf.Sign(Vector2.Dot(
                        new Vector2(-previous.y, previous.x),
                        current));
                    if (lastSign != 0f && sign != lastSign)
                        reversals++;
                    lastSign = sign;
                }
            }

            float area = Mathf.Abs(SignedArea(points)) /
                         Mathf.Max(1f, pathLength * pathLength);

            // Elastico: measure the bend over a window instead of a single
            // sample pair. A real finger/mouse corner is rounded over several
            // frames, so per-point angles can stay tiny even for a clear >/<.
            int turnWindow = Mathf.Clamp(points.Count / 10, 2, 8);
            float maxWindowTurn = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                int before = Mathf.Max(0, i - turnWindow);
                int after = Mathf.Min(points.Count - 1, i + turnWindow);
                Vector2 incoming = points[i] - points[before];
                Vector2 outgoing = points[after] - points[i];
                if (incoming.sqrMagnitude < 1f || outgoing.sqrMagnitude < 1f)
                    continue;

                maxWindowTurn = Mathf.Max(
                    maxWindowTurn, Vector2.Angle(incoming, outgoing));
            }

            Vector2 startDirection = points[Mathf.Min(3, points.Count - 1)] - start;
            Vector2 endDirection = end - points[Mathf.Max(0, points.Count - 4)];
            float overallDirectionChange = 0f;
            if (startDirection.sqrMagnitude > 1f && endDirection.sqrMagnitude > 1f)
                overallDirectionChange = Vector2.Angle(startDirection, endDirection);

            // A straight burst has no bend; a drawn chevron has one.
            if (pathLength > net.magnitude * 1.10f &&
                Mathf.Max(maxWindowTurn, overallDirectionChange) > 25f)
            {
                confidence = 0.86f;
                return DribbleStyle.Elastico;
            }

            if (reversals >= 2 && feintAngle > 55f)
            {
                confidence = 0.82f;
                return DribbleStyle.DoubleTouch;
            }

            if (Mathf.Abs(cumulativeTurn) > 180f && pathLength > net.magnitude * 1.35f)
            {
                confidence = 0.78f;
                return DribbleStyle.Roulette;
            }

            if (feintAngle > 70f && pathLength > net.magnitude * 1.05f)
            {
                confidence = 0.76f;
                return DribbleStyle.Feint;
            }

            if (area > 0.055f && pathLength > net.magnitude * 1.4f)
            {
                confidence = 0.64f;
                return DribbleStyle.StepOver;
            }

            confidence = 0.55f;
            return DribbleStyle.Simple;
        }

        private static float SignedArea(List<Vector2> points)
        {
            float area = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }
    }
}
