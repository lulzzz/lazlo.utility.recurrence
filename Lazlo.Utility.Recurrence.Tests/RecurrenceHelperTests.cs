using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Lazlo.Utility.Recurrence.Tests
{
    [TestClass]
    public class RecurrenceHelperTests
    {
        [TestMethod]
        public void InvalidTimePass()
        {
            try
            {
                string recurr = "[{\"Recurrence\":\"FREQ=MINUTELY;BYMINUTE=0,4,8,12,16,20,24,28,32,36,40,44,48,52,56;X-EWSOFTWARE-DTSTART=00010101T080000Z\"}]";

                var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

                DateTimeOffset justBefore = DateTimeOffset.Parse("3/13/2016 6:59:00 AM +00:00");

                DateTimeOffset drawOpen = RecurrenceHelper.GetNextOccurence(justBefore, "Eastern Standard Time", recurrenceList);
                DateTimeOffset drawClose = RecurrenceHelper.GetNextOccurence(drawOpen, "Eastern Standard Time", recurrenceList);

                DateTimeOffset expectedOpen = DateTimeOffset.Parse("3/13/2016 3:00:00 AM -04:00");
                DateTimeOffset expectedClose = DateTimeOffset.Parse("3/13/2016 3:04:00 AM -04:00");

                Assert.AreEqual<DateTimeOffset>(expectedOpen, drawOpen);
                Assert.AreEqual<DateTimeOffset>(expectedClose, drawClose);
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void FiveYearPassByMinute()
        {
            try
            {
                string recurr = "[{\"Recurrence\":\"FREQ=MINUTELY;BYMINUTE=0,4,8,12,16,20,24,28,32,36,40,44,48,52,56;X-EWSOFTWARE-DTSTART=00010101T080000Z\"}]";

                var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

                DateTimeOffset previous = RecurrenceHelper.GetNextOccurence(DateTimeOffset.UtcNow, "Eastern Standard Time", recurrenceList);

                while (previous < DateTimeOffset.UtcNow.AddYears(5))
                {
                    DateTimeOffset next = RecurrenceHelper.GetNextOccurence(previous, "Eastern Standard Time", recurrenceList);

                    if ((next - previous).TotalMinutes != 4)
                    {
                        Assert.Fail();
                    }

                    previous = next;
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void FiveYearPassBySecond()
        {
            try
            {
                string recurr = "[{\"Recurrence\":\"FREQ=SECONDLY;BYSECOND=50;X-EWSOFTWARE-DTSTART=20160108T210153Z\"}]";

                var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

                DateTimeOffset previous = RecurrenceHelper.GetNextOccurence(DateTimeOffset.UtcNow, "Eastern Standard Time", recurrenceList);

                while (previous < DateTimeOffset.UtcNow.AddYears(5))
                {
                    DateTimeOffset next = RecurrenceHelper.GetNextOccurence(previous, "Eastern Standard Time", recurrenceList);

                    if ((next - previous).TotalMinutes != 1)
                    {
                        Assert.Fail();
                    }

                    previous = next;
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void FiveYearPassByWeek()
        {
            try
            {
                string timeZoneId = "Eastern Standard Time";

                TimeZoneInfo authorityTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

                int hour = 21;

                string recurr = $"[{{\"Recurrence\":\"FREQ=WEEKLY;BYDAY=SA;BYHOUR={hour}\"}}]";

                var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

                DateTimeOffset previous = RecurrenceHelper.GetNextOccurence(DateTimeOffset.UtcNow, timeZoneId, recurrenceList);

                while (previous < DateTimeOffset.UtcNow.AddYears(5))
                {
                    DateTimeOffset next = RecurrenceHelper.GetNextOccurence(previous, timeZoneId, recurrenceList);

                    DateTime authorityTime = TimeZoneInfo.ConvertTimeFromUtc(next.UtcDateTime, authorityTimeZone);

                    Debug.WriteLine($"{authorityTime} {next.Offset}");

                    if (authorityTime.Hour != hour)
                    {
                        Assert.Fail();
                    }

                    if (authorityTime.DayOfWeek != DayOfWeek.Saturday)
                    {
                        Assert.Fail();
                    }

                    previous = next;
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void AmbiguousTimeSuccess()
        {
            string recurr = "[{\"Recurrence\":\"FREQ=MINUTELY;BYMINUTE=0,4,8,12,16,20,24,28,32,36,40,44,48,52,56;X-EWSOFTWARE-DTSTART=00010101T080000Z\"}]";

            var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

            DateTimeOffset justBefore = DateTimeOffset.Parse("11/6/2016 4:59:00 AM +00:00");

            DateTimeOffset drawOpen = RecurrenceHelper.GetNextOccurence(justBefore, "Eastern Standard Time", recurrenceList);
            DateTimeOffset drawClose = RecurrenceHelper.GetNextOccurence(drawOpen, "Eastern Standard Time", recurrenceList);

            DateTimeOffset expectedOpen = DateTimeOffset.Parse("11/6/2016 1:00:00 AM -04:00");
            DateTimeOffset expectedClose = DateTimeOffset.Parse("11/6/2016 1:04:00 AM -04:00");

            Assert.AreEqual<DateTimeOffset>(expectedOpen, drawOpen);
            Assert.AreEqual<DateTimeOffset>(expectedClose, drawClose);

            justBefore = DateTimeOffset.Parse("11/6/2016 5:59:00 AM +00:00");

            drawOpen = RecurrenceHelper.GetNextOccurence(justBefore, "Eastern Standard Time", recurrenceList);
            drawClose = RecurrenceHelper.GetNextOccurence(drawOpen, "Eastern Standard Time", recurrenceList);

            expectedOpen = DateTimeOffset.Parse("11/6/2016 1:00:00 AM -05:00");
            expectedClose = DateTimeOffset.Parse("11/6/2016 1:04:00 AM -05:00");

            Assert.AreEqual<DateTimeOffset>(expectedOpen, drawOpen);
            Assert.AreEqual<DateTimeOffset>(expectedClose, drawClose);

            justBefore = DateTimeOffset.Parse("11/6/2016 6:59:00 AM +00:00");

            drawOpen = RecurrenceHelper.GetNextOccurence(justBefore, "Eastern Standard Time", recurrenceList);
            drawClose = RecurrenceHelper.GetNextOccurence(drawOpen, "Eastern Standard Time", recurrenceList);

            expectedOpen = DateTimeOffset.Parse("11/6/2016 2:00:00 AM -05:00");
            expectedClose = DateTimeOffset.Parse("11/6/2016 2:04:00 AM -05:00");

            Assert.AreEqual<DateTimeOffset>(expectedOpen, drawOpen);
            Assert.AreEqual<DateTimeOffset>(expectedClose, drawClose);
        }

        [TestMethod]
        public void OccurrsOnSuccess()
        {
            try
            {
                string recurr = "[{\"Recurrence\":\"FREQ=MINUTELY;BYMINUTE=0,4,8,12,16,20,24,28,32,36,40,44,48,52,56;X-EWSOFTWARE-DTSTART=00010101T080000Z\"}]";

                var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

                DateTimeOffset now = new DateTimeOffset(2019, 1, 1, 1, 4, 0, TimeSpan.Zero);

                bool occursOn = RecurrenceHelper.OccursOn(recurrenceList, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"), now);

                Assert.IsTrue(occursOn);
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void PreviousSuccess()
        {
            try
            {
                string recurr = "[{\"Recurrence\":\"FREQ=MINUTELY;BYMINUTE=0,4,8,12,16,20,24,28,32,36,40,44,48,52,56;X-EWSOFTWARE-DTSTART=00010101T080000Z\"}]";

                var recurrenceList = JsonConvert.DeserializeObject<List<Recurrence>>(recurr);

                DateTimeOffset previous = RecurrenceHelper.GetPreviousOccurence(DateTimeOffset.UtcNow, "Eastern Standard Time", recurrenceList);
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Assert.Fail();
            }
        }
    }
}
