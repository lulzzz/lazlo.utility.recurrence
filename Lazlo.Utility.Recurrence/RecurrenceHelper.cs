using System;
using System.Collections.Generic;
using System.Linq;

namespace Lazlo.Utility.Recurrence
{
    public class RecurrenceHelper
    {
        /// <summary>
        /// Given a recurrence rule, get the next valid draw open and close based on a time zone. In the situation of DST
        /// ending, find the minimum of the ambigious times which comes after the afterTime argument. In the situation of DST
        /// starting, simply skip an invalid time provided by the library
        /// </summary>
        /// <param name="afterTime">The UTC time after which you want the next recurrence match</param>
        /// <param name="timeZoneId">The time zone you are calculatinig for</param>
        /// <param name="recurrence">The recurrence pattern</param>
        /// <returns>The next occurence in UTC</returns>
        public static DateTimeOffset GetNextOccurence(DateTimeOffset afterTime, string timeZoneId, List<Recurrence> recurrenceList)
        {
            DateTimeOffset nextUtc;

            TimeZoneInfo authorityTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            DateTime afterTimeLocalized = TimeZoneInfo.ConvertTime(afterTime, authorityTimeZone).DateTime;

            DateTime nextLocal = recurrenceList.Min(e => e.NextInstance(afterTimeLocalized, true));

            if (authorityTimeZone.IsInvalidTime(nextLocal))
            {
                // Invalid times occur when Daylight Savings starts and the clocks are moved forward
                // I.E. The clock is 1:59:59AM ST and becomes 3:00:00AM DST, thereby eliminating the hour from 2AM to 3AM
                // Unfortunately our recurrance library will give us 2:00:00AM as a valid time
                // Thus we must add an hour

                nextLocal = nextLocal.AddHours(1);

                nextUtc = new DateTimeOffset(nextLocal, authorityTimeZone.GetUtcOffset(nextLocal));
            }

            else if (authorityTimeZone.IsAmbiguousTime(nextLocal))
            {
                // Ambigious times occur when Daylight Savings ends and the clocks are moved backward
                // I.E. The clock is 1:59:59AM DST and becomes 1:00:00AM ST, thereby repeating the hour from 1AM to 2AM
                // In this situation, the recurrence library will not repeat the hour

                var minOffset = (from Offset in authorityTimeZone.GetAmbiguousTimeOffsets(nextLocal)
                                 let DrawOpen = new DateTimeOffset(nextLocal, Offset)
                                 where DrawOpen > afterTime
                                 orderby DrawOpen
                                 select new
                                 {
                                     DrawOpen,
                                     Offset
                                 }).First();

                nextUtc = minOffset.DrawOpen;
            }

            else if (authorityTimeZone.IsAmbiguousTime(nextLocal.AddHours(-1)) && recurrenceList.Any(z => z.OccursOn(nextLocal.AddHours(-1), true)))
            {
                // In this situation, the recurrence library potentially skipped the 2nd ambigious instance
                // So apply the two UTC time offsets and see if any of them is greater than the afterTime argument
                // If so, the recurrence library skipped the 2nd ambigious instance, and potentiallyValidTime is valid.
                // If not, nextLocal is correct

                DateTime potentiallyValidTime = nextLocal.AddHours(-1);

                var minOffset = (from Offset in authorityTimeZone.GetAmbiguousTimeOffsets(potentiallyValidTime)
                                 let DrawOpen = new DateTimeOffset(potentiallyValidTime, Offset)
                                 where DrawOpen > afterTime
                                 orderby DrawOpen
                                 select new
                                 {
                                     DrawOpen,
                                     Offset
                                 }).FirstOrDefault();

                if (minOffset == null)
                {
                    nextUtc = new DateTimeOffset(nextLocal, authorityTimeZone.GetUtcOffset(nextLocal));
                }

                else
                {
                    nextUtc = minOffset.DrawOpen;
                }
            }

            else
            {
                nextUtc = new DateTimeOffset(nextLocal, authorityTimeZone.GetUtcOffset(nextLocal));
            }

            return nextUtc;
        }

        public static bool OccursOn(List<Recurrence> recurrenceList, TimeZoneInfo timeZone, DateTimeOffset utcTime)
        {
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime.UtcDateTime, timeZone);

            if (recurrenceList == null)
            {
                throw new ArgumentException($"{nameof(recurrenceList)} cannot be null");
            }

            return recurrenceList.Any(z => z.OccursOn(localTime, true));
        }

        public static DateTimeOffset GetPreviousOccurence(DateTimeOffset beforeTime, string timeZoneId, List<Recurrence> recurrenceList)
        {
            DateTimeOffset next = RecurrenceHelper.GetNextOccurence(DateTimeOffset.UtcNow, timeZoneId, recurrenceList);
            DateTimeOffset afterNext = RecurrenceHelper.GetNextOccurence(next, timeZoneId, recurrenceList);

            // This is basically a guess for how far back we should skip in each loop
            TimeSpan betweenDraws = afterNext - next;

            DateTimeOffset target = beforeTime - betweenDraws;

            while (true)
            {
                DateTimeOffset candidate = GetNextOccurence(target, timeZoneId, recurrenceList);

                if (candidate < beforeTime)
                {
                    // We've gone far enough back, but have we gone too far?

                    while (true)
                    {
                        DateTimeOffset afterCandidate = GetNextOccurence(candidate, timeZoneId, recurrenceList);

                        if (afterCandidate < beforeTime)
                        {
                            candidate = afterCandidate;
                        }

                        else
                        {
                            return candidate;
                        }
                    }
                }

                target = target - betweenDraws;
            }
        }
    }
}
