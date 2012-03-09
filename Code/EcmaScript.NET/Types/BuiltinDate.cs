//------------------------------------------------------------------------------
// <license file="NativeDate.cs">
//     
//      The use and distribution terms for this software are contained in the file
//      named 'LICENSE', which can be found in the resources directory of this
//		distribution.
//
//      By using this software in any fashion, you are agreeing to be bound by the
//      terms of this license.
//     
// </license>                                                                
//------------------------------------------------------------------------------


using System;
using System.Globalization;

namespace EcmaScript.NET.Types
{

    /// <summary> This class implements the Date native object.
    /// See ECMA 15.9.
    /// </summary>
    sealed class BuiltinDate : IdScriptableObject
    {
        override public string ClassName
        {
            get
            {
                return "Date";
            }

        }
        internal double JSTimeValue
        {
            get
            {
                return date;
            }

        }


        private static readonly object DATE_TAG = new object ();

        private const string js_NaN_date_str = "Invalid Date";

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinDate obj = new BuiltinDate ();
            // Set the value of the prototype Date to NaN ('invalid date');
            obj.date = double.NaN;
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed
                , ScriptableObject.DONTENUM | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        private BuiltinDate ()
        {
            if (thisTimeZone == null) {
                thisTimeZone = System.TimeZone.CurrentTimeZone;
                LocalTZA = 1 * msPerHour; // TODO: FIXME
            }
        }

        public override object GetDefaultValue (Type typeHint)
        {
            if (typeHint == null)
                typeHint = typeof (string);
            return base.GetDefaultValue (typeHint);
        }

        protected internal override void FillConstructorProperties (IdFunctionObject ctor)
        {
            AddIdFunctionProperty (ctor, DATE_TAG, ConstructorId_now, "now", 0);
            AddIdFunctionProperty (ctor, DATE_TAG, ConstructorId_parse, "parse", 1);
            AddIdFunctionProperty (ctor, DATE_TAG, ConstructorId_UTC, "UTC", 1);
            base.FillConstructorProperties (ctor);
        }

        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            switch (id) {

                case Id_constructor:
                    arity = 1;
                    s = "constructor";
                    break;

                case Id_toString:
                    arity = 0;
                    s = "toString";
                    break;

                case Id_toTimeString:
                    arity = 0;
                    s = "toTimeString";
                    break;

                case Id_toDateString:
                    arity = 0;
                    s = "toDateString";
                    break;

                case Id_toLocaleString:
                    arity = 0;
                    s = "toLocaleString";
                    break;

                case Id_toLocaleTimeString:
                    arity = 0;
                    s = "toLocaleTimeString";
                    break;

                case Id_toLocaleDateString:
                    arity = 0;
                    s = "toLocaleDateString";
                    break;

                case Id_toUTCString:
                    arity = 0;
                    s = "toUTCString";
                    break;

                case Id_toSource:
                    arity = 0;
                    s = "toSource";
                    break;

                case Id_valueOf:
                    arity = 0;
                    s = "valueOf";
                    break;

                case Id_getTime:
                    arity = 0;
                    s = "getTime";
                    break;

                case Id_getYear:
                    arity = 0;
                    s = "getYear";
                    break;

                case Id_getFullYear:
                    arity = 0;
                    s = "getFullYear";
                    break;

                case Id_getUTCFullYear:
                    arity = 0;
                    s = "getUTCFullYear";
                    break;

                case Id_getMonth:
                    arity = 0;
                    s = "getMonth";
                    break;

                case Id_getUTCMonth:
                    arity = 0;
                    s = "getUTCMonth";
                    break;

                case Id_getDate:
                    arity = 0;
                    s = "getDate";
                    break;

                case Id_getUTCDate:
                    arity = 0;
                    s = "getUTCDate";
                    break;

                case Id_getDay:
                    arity = 0;
                    s = "getDay";
                    break;

                case Id_getUTCDay:
                    arity = 0;
                    s = "getUTCDay";
                    break;

                case Id_getHours:
                    arity = 0;
                    s = "getHours";
                    break;

                case Id_getUTCHours:
                    arity = 0;
                    s = "getUTCHours";
                    break;

                case Id_getMinutes:
                    arity = 0;
                    s = "getMinutes";
                    break;

                case Id_getUTCMinutes:
                    arity = 0;
                    s = "getUTCMinutes";
                    break;

                case Id_getSeconds:
                    arity = 0;
                    s = "getSeconds";
                    break;

                case Id_getUTCSeconds:
                    arity = 0;
                    s = "getUTCSeconds";
                    break;

                case Id_getMilliseconds:
                    arity = 0;
                    s = "getMilliseconds";
                    break;

                case Id_getUTCMilliseconds:
                    arity = 0;
                    s = "getUTCMilliseconds";
                    break;

                case Id_getTimezoneOffset:
                    arity = 0;
                    s = "getTimezoneOffset";
                    break;

                case Id_setTime:
                    arity = 1;
                    s = "setTime";
                    break;

                case Id_setMilliseconds:
                    arity = 1;
                    s = "setMilliseconds";
                    break;

                case Id_setUTCMilliseconds:
                    arity = 1;
                    s = "setUTCMilliseconds";
                    break;

                case Id_setSeconds:
                    arity = 2;
                    s = "setSeconds";
                    break;

                case Id_setUTCSeconds:
                    arity = 2;
                    s = "setUTCSeconds";
                    break;

                case Id_setMinutes:
                    arity = 3;
                    s = "setMinutes";
                    break;

                case Id_setUTCMinutes:
                    arity = 3;
                    s = "setUTCMinutes";
                    break;

                case Id_setHours:
                    arity = 4;
                    s = "setHours";
                    break;

                case Id_setUTCHours:
                    arity = 4;
                    s = "setUTCHours";
                    break;

                case Id_setDate:
                    arity = 1;
                    s = "setDate";
                    break;

                case Id_setUTCDate:
                    arity = 1;
                    s = "setUTCDate";
                    break;

                case Id_setMonth:
                    arity = 2;
                    s = "setMonth";
                    break;

                case Id_setUTCMonth:
                    arity = 2;
                    s = "setUTCMonth";
                    break;

                case Id_setFullYear:
                    arity = 3;
                    s = "setFullYear";
                    break;

                case Id_setUTCFullYear:
                    arity = 3;
                    s = "setUTCFullYear";
                    break;

                case Id_setYear:
                    arity = 1;
                    s = "setYear";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (DATE_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (DATE_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case ConstructorId_now:
                    return now ();


                case ConstructorId_parse: {
                        string dataStr = ScriptConvert.ToString (args, 0);
                        return date_parseString (dataStr);
                    }


                case ConstructorId_UTC:
                    return jsStaticFunction_UTC (args);


                case Id_constructor: {
                        // if called as a function, just return a string
                        // representing the current time.
                        if (thisObj != null)
                            return date_format (now (), Id_toString);
                        return jsConstructor (args);
                    }
            }

            // The rest of Date.prototype methods require thisObj to be Date

            if (!(thisObj is BuiltinDate))
                throw IncompatibleCallError (f);
            BuiltinDate realThis = (BuiltinDate)thisObj;
            double t = realThis.date;

            switch (id) {


                case Id_toString:
                case Id_toTimeString:
                case Id_toDateString:
                    if (!double.IsNaN (t)) {
                        return date_format (t, id);
                    }
                    return js_NaN_date_str;


                case Id_toLocaleString:
                case Id_toLocaleTimeString:
                case Id_toLocaleDateString:
                    if (!double.IsNaN (t)) {
                        return toLocale_helper (t, id);
                    }
                    return js_NaN_date_str;


                case Id_toUTCString:
                    if (!double.IsNaN (t)) {
                        return js_toUTCString (t);
                    }
                    return js_NaN_date_str;


                case Id_toSource:
                    return "(new Date(" + ScriptConvert.ToString (t) + "))";


                case Id_valueOf:
                case Id_getTime:
                    return t;


                case Id_getYear:
                case Id_getFullYear:
                case Id_getUTCFullYear:
                    if (!double.IsNaN (t)) {
                        if (id != Id_getUTCFullYear)
                            t = LocalTime (t);
                        t = YearFromTime (t);
                        if (id == Id_getYear) {
                            if (cx.HasFeature (Context.Features.NonEcmaGetYear)) {
                                if (1900 <= t && t < 2000) {
                                    t -= 1900;
                                }
                            }
                            else {
                                t -= 1900;
                            }
                        }
                    }
                    return (t);


                case Id_getMonth:
                case Id_getUTCMonth:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getMonth)
                            t = LocalTime (t);
                        t = MonthFromTime (t);
                    }
                    return (t);


                case Id_getDate:
                case Id_getUTCDate:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getDate)
                            t = LocalTime (t);
                        t = DateFromTime (t);
                    }
                    return (t);


                case Id_getDay:
                case Id_getUTCDay:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getDay)
                            t = LocalTime (t);
                        t = WeekDay (t);
                    }
                    return (t);


                case Id_getHours:
                case Id_getUTCHours:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getHours)
                            t = LocalTime (t);
                        t = HourFromTime (t);
                    }
                    return (t);


                case Id_getMinutes:
                case Id_getUTCMinutes:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getMinutes)
                            t = LocalTime (t);
                        t = MinFromTime (t);
                    }
                    return (t);


                case Id_getSeconds:
                case Id_getUTCSeconds:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getSeconds)
                            t = LocalTime (t);
                        t = SecFromTime (t);
                    }
                    return (t);


                case Id_getMilliseconds:
                case Id_getUTCMilliseconds:
                    if (!double.IsNaN (t)) {
                        if (id == Id_getMilliseconds)
                            t = LocalTime (t);
                        t = msFromTime (t);
                    }
                    return (t);


                case Id_getTimezoneOffset:
                    if (!double.IsNaN (t)) {
                        t = (t - LocalTime (t)) / msPerMinute;
                    }
                    return (t);


                case Id_setTime:
                    t = TimeClip (ScriptConvert.ToNumber (args, 0));
                    realThis.date = t;
                    return t;


                case Id_setMilliseconds:
                case Id_setUTCMilliseconds:
                case Id_setSeconds:
                case Id_setUTCSeconds:
                case Id_setMinutes:
                case Id_setUTCMinutes:
                case Id_setHours:
                case Id_setUTCHours:
                    t = makeTime (t, args, id);
                    realThis.date = t;
                    return (t);


                case Id_setDate:
                case Id_setUTCDate:
                case Id_setMonth:
                case Id_setUTCMonth:
                case Id_setFullYear:
                case Id_setUTCFullYear:
                    t = makeDate (t, args, id);
                    realThis.date = t;
                    return (t);


                case Id_setYear: {
                        double year = ScriptConvert.ToNumber (args, 0);

                        if (double.IsNaN (year) || double.IsInfinity (year)) {
                            t = double.NaN;
                        }
                        else {
                            if (double.IsNaN (t)) {
                                t = 0;
                            }
                            else {
                                t = LocalTime (t);
                            }

                            if (year >= 0 && year <= 99)
                                year += 1900;

                            double day = MakeDay (year, MonthFromTime (t), DateFromTime (t));
                            t = MakeDate (day, TimeWithinDay (t));
                            t = internalUTC (t);
                            t = TimeClip (t);
                        }
                    }
                    realThis.date = t;
                    return (t);


                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
        }

        /* ECMA helper functions */

        private const double HalfTimeDomain = 8.64e15;
        private const double HoursPerDay = 24.0;
        private const double MinutesPerHour = 60.0;
        private const double SecondsPerMinute = 60.0;
        private const double msPerSecond = 1000.0;

        private static readonly double MinutesPerDay = (HoursPerDay * MinutesPerHour);
        private static readonly double SecondsPerDay = (MinutesPerDay * SecondsPerMinute);
        private static readonly double SecondsPerHour = (MinutesPerHour * SecondsPerMinute);
        private static readonly double msPerDay = (SecondsPerDay * msPerSecond);
        private static readonly double msPerHour = (SecondsPerHour * msPerSecond);
        private static readonly double msPerMinute = (SecondsPerMinute * msPerSecond);

        private static double Day (double t)
        {
            return Math.Floor (t / msPerDay);
        }

        private static double TimeWithinDay (double t)
        {
            double result;
            result = t % msPerDay;
            if (result < 0)
                result += msPerDay;
            return result;
        }

        private static bool IsLeapYear (int year)
        {
            return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
        }

        /* math here has to be f.p, because we need
        *  floor((1968 - 1969) / 4) == -1
        */
        private static double DayFromYear (double y)
        {
            return ((365 * ((y) - 1970) + Math.Floor (((y) - 1969) / 4.0) - Math.Floor (((y) - 1901) / 100.0) + Math.Floor (((y) - 1601) / 400.0)));
        }

        private static double TimeFromYear (double y)
        {
            return DayFromYear (y) * msPerDay;
        }

        private static int YearFromTime (double t)
        {
            int lo = (int)Math.Floor ((t / msPerDay) / 366) + 1970;
            int hi = (int)Math.Floor ((t / msPerDay) / 365) + 1970;
            int mid;

            /* above doesn't work for negative dates... */
            if (hi < lo) {
                int temp = lo;
                lo = hi;
                hi = temp;
            }

            /* Use a simple binary search algorithm to find the right
            year.  This seems like brute force... but the computation
            of hi and lo years above lands within one year of the
            correct answer for years within a thousand years of
            1970; the loop below only requires six iterations
            for year 270000. */
            while (hi > lo) {
                mid = (hi + lo) / 2;
                if (TimeFromYear (mid) > t) {
                    hi = mid - 1;
                }
                else {
                    lo = mid + 1;
                    if (TimeFromYear (lo) > t) {
                        return mid;
                    }
                }
            }
            return lo;
        }

        private static bool InLeapYear (double t)
        {
            return IsLeapYear (YearFromTime (t));
        }

        private static double DayFromMonth (int m, int year)
        {
            int day = m * 30;

            if (m >= 7) {
                day += m / 2 - 1;
            }
            else if (m >= 2) {
                day += (m - 1) / 2 - 1;
            }
            else {
                day += m;
            }

            if (m >= 2 && IsLeapYear (year)) {
                ++day;
            }

            return day;
        }

        private static int MonthFromTime (double t)
        {
            int year = YearFromTime (t);
            int d = (int)(Day (t) - DayFromYear (year));

            d -= (31 + 28);
            if (d < 0) {
                return (d < -28) ? 0 : 1;
            }

            if (IsLeapYear (year)) {
                if (d == 0)
                    return 1; // 29 February
                --d;
            }

            // d: date count from 1 March
            int estimate = d / 30; // approx number of month since March
            int mstart;
            switch (estimate) {

                case 0:
                    return 2;

                case 1:
                    mstart = 31;
                    break;

                case 2:
                    mstart = 31 + 30;
                    break;

                case 3:
                    mstart = 31 + 30 + 31;
                    break;

                case 4:
                    mstart = 31 + 30 + 31 + 30;
                    break;

                case 5:
                    mstart = 31 + 30 + 31 + 30 + 31;
                    break;

                case 6:
                    mstart = 31 + 30 + 31 + 30 + 31 + 31;
                    break;

                case 7:
                    mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30;
                    break;

                case 8:
                    mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31;
                    break;

                case 9:
                    mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31 + 30;
                    break;

                case 10:
                    return 11; //Late december

                default:
                    throw Context.CodeBug ();

            }
            // if d < mstart then real month since March == estimate - 1
            return (d >= mstart) ? estimate + 2 : estimate + 1;
        }

        private static int DateFromTime (double t)
        {
            int year = YearFromTime (t);
            int d = (int)(Day (t) - DayFromYear (year));

            d -= (31 + 28);
            if (d < 0) {
                return (d < -28) ? d + 31 + 28 + 1 : d + 28 + 1;
            }

            if (IsLeapYear (year)) {
                if (d == 0)
                    return 29; // 29 February
                --d;
            }

            // d: date count from 1 March
            int mdays, mstart;
            switch (d / 30) {

                // approx number of month since March
                case 0:
                    return d + 1;

                case 1:
                    mdays = 31;
                    mstart = 31;
                    break;

                case 2:
                    mdays = 30;
                    mstart = 31 + 30;
                    break;

                case 3:
                    mdays = 31;
                    mstart = 31 + 30 + 31;
                    break;

                case 4:
                    mdays = 30;
                    mstart = 31 + 30 + 31 + 30;
                    break;

                case 5:
                    mdays = 31;
                    mstart = 31 + 30 + 31 + 30 + 31;
                    break;

                case 6:
                    mdays = 31;
                    mstart = 31 + 30 + 31 + 30 + 31 + 31;
                    break;

                case 7:
                    mdays = 30;
                    mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30;
                    break;

                case 8:
                    mdays = 31;
                    mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31;
                    break;

                case 9:
                    mdays = 30;
                    mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31 + 30;
                    break;

                case 10:
                    return d - (31 + 30 + 31 + 30 + 31 + 31 + 30 + 31 + 30) + 1; //Late december

                default:
                    throw Context.CodeBug ();

            }
            d -= mstart;
            if (d < 0) {
                // wrong estimate: sfhift to previous month
                d += mdays;
            }
            return d + 1;
        }

        private static int WeekDay (double t)
        {
            double result;
            result = Day (t) + 4;
            result = result % 7;
            if (result < 0)
                result += 7;
            return (int)result;
        }

        private static double now ()
        {
            return (double)((DateTime.Now.Ticks - 621355968000000000) / 10000);
        }

        private static double DaylightSavingTA (double t)
        {
            // Another workaround!  The JRE doesn't seem to know about DST
            // before year 1 AD, so we map to equivalent dates for the
            // purposes of finding dst.  To be safe, we do this for years
            // outside 1970-2038.
            if (t < 0.0 || t > 2145916800000.0) {
                int year = EquivalentYear (YearFromTime (t));
                double day = MakeDay (year, MonthFromTime (t), DateFromTime (t));
                t = MakeDate (day, TimeWithinDay (t));
            }

            DateTime date = FromMilliseconds (t);
            if (thisTimeZone.IsDaylightSavingTime (date))
                return msPerHour;
            else
                return 0;

        }

        /*
        * Find a year for which any given date will fall on the same weekday.
        *
        * This function should be used with caution when used other than
        * for determining DST; it hasn't been proven not to produce an
        * incorrect year for times near year boundaries.
        */
        private static int EquivalentYear (int year)
        {
            int day = (int)DayFromYear (year) + 4;
            day = day % 7;
            if (day < 0)
                day += 7;
            // Years and leap years on which Jan 1 is a Sunday, Monday, etc.
            if (IsLeapYear (year)) {
                switch (day) {

                    case 0:
                        return 1984;

                    case 1:
                        return 1996;

                    case 2:
                        return 1980;

                    case 3:
                        return 1992;

                    case 4:
                        return 1976;

                    case 5:
                        return 1988;

                    case 6:
                        return 1972;
                }
            }
            else {
                switch (day) {

                    case 0:
                        return 1978;

                    case 1:
                        return 1973;

                    case 2:
                        return 1974;

                    case 3:
                        return 1975;

                    case 4:
                        return 1981;

                    case 5:
                        return 1971;

                    case 6:
                        return 1977;
                }
            }
            // Unreachable
            throw Context.CodeBug ();
        }

        private static double LocalTime (double t)
        {
            return t + LocalTZA + DaylightSavingTA (t);
        }

        private static double internalUTC (double t)
        {
            return t - LocalTZA - DaylightSavingTA (t - LocalTZA);
        }

        private static int HourFromTime (double t)
        {
            double result;
            result = Math.Floor (t / msPerHour) % HoursPerDay;
            if (result < 0)
                result += HoursPerDay;
            return (int)result;
        }

        private static int MinFromTime (double t)
        {
            double result;
            result = Math.Floor (t / msPerMinute) % MinutesPerHour;
            if (result < 0)
                result += MinutesPerHour;
            return (int)result;
        }

        private static int SecFromTime (double t)
        {
            double result;
            result = Math.Floor (t / msPerSecond) % SecondsPerMinute;
            if (result < 0)
                result += SecondsPerMinute;
            return (int)result;
        }

        private static int msFromTime (double t)
        {
            double result;
            result = t % msPerSecond;
            if (result < 0)
                result += msPerSecond;
            return (int)result;
        }

        private static double MakeTime (double hour, double min, double sec, double ms)
        {
            return ((hour * MinutesPerHour + min) * SecondsPerMinute + sec) * msPerSecond + ms;
        }

        private static double MakeDay (double year, double month, double date)
        {
            year += Math.Floor (month / 12);

            month = month % 12;
            if (month < 0)
                month += 12;

            double yearday = Math.Floor (TimeFromYear (year) / msPerDay);
            double monthday = DayFromMonth ((int)month, (int)year);
            return yearday + monthday + date - 1;
        }

        private static double MakeDate (double day, double time)
        {
            return day * msPerDay + time;
        }

        private static double TimeClip (double d)
        {
            if (double.IsNaN (d) || d == System.Double.PositiveInfinity || d == System.Double.NegativeInfinity || Math.Abs (d) > HalfTimeDomain) {
                return double.NaN;
            }
            if (d > 0.0)
                return Math.Floor (d + 0.0);
            else
                return Math.Ceiling (d + 0.0);
        }

        /* end of ECMA helper functions */

        /* find UTC time from given date... no 1900 correction! */
        private static double date_msecFromDate (double year, double mon, double mday, double hour, double min, double sec, double msec)
        {
            double day;
            double time;
            double result;

            day = MakeDay (year, mon, mday);
            time = MakeTime (hour, min, sec, msec);
            result = MakeDate (day, time);
            return result;
        }


        private const int MAXARGS = 7;
        private static double jsStaticFunction_UTC (object [] args)
        {
            double [] array = new double [MAXARGS];
            int loop;
            double d;

            for (loop = 0; loop < MAXARGS; loop++) {
                if (loop < args.Length) {
                    d = ScriptConvert.ToNumber (args [loop]);
                    if (double.IsNaN (d) || System.Double.IsInfinity (d)) {
                        return double.NaN;
                    }
                    array [loop] = ScriptConvert.ToInteger (args [loop]);
                }
                else {
                    array [loop] = 0;
                }
            }

            /* adjust 2-digit years into the 20th century */
            if (array [0] >= 0 && array [0] <= 99)
                array [0] += 1900;

            /* if we got a 0 for 'date' (which is out of range)
            * pretend it's a 1.  (So Date.UTC(1972, 5) works) */
            if (array [2] < 1)
                array [2] = 1;

            d = date_msecFromDate (array [0], array [1], array [2], array [3], array [4], array [5], array [6]);
            d = TimeClip (d);
            return d;
        }

        public static double date_parseString (string s)
        {
            int year = -1;
            int mon = -1;
            int mday = -1;
            int hour = -1;
            int min = -1;
            int sec = -1;
            char c = (char)(0);
            char si = (char)(0);
            int i = 0;
            int n = -1;
            double tzoffset = -1;
            char prevc = (char)(0);
            int limit = 0;
            bool seenplusminus = false;

            limit = s.Length;
            while (i < limit) {
                c = s [i];
                i++;
                if (c <= ' ' || c == ',' || c == '-') {
                    if (i < limit) {
                        si = s [i];
                        if (c == '-' && '0' <= si && si <= '9') {
                            prevc = c;
                        }
                    }
                    continue;
                }
                if (c == '(') {
                    /* comments) */
                    int depth = 1;
                    while (i < limit) {
                        c = s [i];
                        i++;
                        if (c == '(')
                            depth++;
                        else if (c == ')')
                            if (--depth <= 0)
                                break;
                    }
                    continue;
                }
                if ('0' <= c && c <= '9') {
                    n = c - '0';
                    while (i < limit && '0' <= (c = s [i]) && c <= '9') {
                        n = n * 10 + c - '0';
                        i++;
                    }

                    /* allow TZA before the year, so
                    * 'Wed Nov 05 21:49:11 GMT-0800 1997'
                    * works */

                    /* uses of seenplusminus allow : in TZA, so Java
                    * no-timezone style of GMT+4:30 works
                    */
                    if ((prevc == '+' || prevc == '-')) {
                        /* make ':' case below change tzoffset */
                        seenplusminus = true;

                        /* offset */
                        if (n < 24)
                            n = n * 60;
                        /* EG. "GMT-3" */
                        else
                            n = n % 100 + n / 100 * 60; /* eg "GMT-0430" */
                        if (prevc == '+')
                            /* plus means east of GMT */
                            n = -n;
                        if (tzoffset != 0 && tzoffset != -1)
                            return double.NaN;
                        tzoffset = n;
                    }
                    else if (n >= 70 || (prevc == '/' && mon >= 0 && mday >= 0 && year < 0)) {
                        if (mday < 0)
                            mday = n;
                        else if (year >= 0)
                            return double.NaN;
                        else if (c <= ' ' || c == ',' || c == '/' || i >= limit)
                            year = n < 100 ? n + 1900 : n;
                        else
                            return double.NaN;
                    }
                    else if (c == ':') {
                        if (hour < 0)
                            hour = n;
                        else if (min < 0)
                            min = n;
                        else
                            return double.NaN;
                    }
                    else if (c == '/') {
                        if (mon < 0)
                            mon = n - 1;
                        else if (mday < 0)
                            mday = n;
                        else
                            return double.NaN;
                    }
                    else if (i < limit && c != ',' && c > ' ' && c != '-') {
                        return double.NaN;
                    }
                    else if (seenplusminus && n < 60) {
                        /* handle GMT-3:30 */
                        if (tzoffset < 0)
                            tzoffset -= n;
                        else
                            tzoffset += n;
                    }
                    else if (hour >= 0 && min < 0) {
                        min = n;
                    }
                    else if (min >= 0 && sec < 0) {
                        sec = n;
                    }
                    else if (mday < 0) {
                        mday = n;
                    }
                    else {
                        return double.NaN;
                    }
                    prevc = (char)(0);
                }
                else if (c == '/' || c == ':' || c == '+' || c == '-') {
                    prevc = c;
                }
                else {
                    int st = i - 1;
                    while (i < limit) {
                        c = s [i];
                        if (!(('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z')))
                            break;
                        i++;
                    }
                    int letterCount = i - st;
                    if (letterCount < 2)
                        return double.NaN;
                    /*
                    * Use ported code from jsdate.c rather than the locale-specific
                    * date-parsing code from Java, to keep js and rhino consistent.
                    * Is this the right strategy?
                    */
                    string wtb = "am;pm;" + "monday;tuesday;wednesday;thursday;friday;" + "saturday;sunday;" + "january;february;march;april;may;june;" + "july;august;september;october;november;december;" + "gmt;ut;utc;est;edt;cst;cdt;mst;mdt;pst;pdt;";
                    int index = 0;
                    for (int wtbOffset = 0; ; ) {
                        int wtbNext = wtb.IndexOf (';', wtbOffset);
                        if (wtbNext < 0)
                            return double.NaN;
                        if (String.Compare (wtb, wtbOffset, s, st, letterCount, true) == 0)
                            break;
                        wtbOffset = wtbNext + 1;
                        ++index;
                    }
                    if (index < 2) {
                        /*
                        * AM/PM. Count 12:30 AM as 00:30, 12:30 PM as
                        * 12:30, instead of blindly adding 12 if PM.
                        */
                        if (hour > 12 || hour < 0) {
                            return double.NaN;
                        }
                        else if (index == 0) {
                            // AM
                            if (hour == 12)
                                hour = 0;
                        }
                        else {
                            // PM
                            if (hour != 12)
                                hour += 12;
                        }
                    }
                    else if ((index -= 2) < 7) {
                        // ignore week days
                    }
                    else if ((index -= 7) < 12) {
                        // month
                        if (mon < 0) {
                            mon = index;
                        }
                        else {
                            return double.NaN;
                        }
                    }
                    else {
                        index -= 12;
                        // timezones
                        switch (index) {

                            case 0:
                                tzoffset = 0;
                                break;

                            case 1:
                                tzoffset = 0;
                                break;

                            case 2:
                                tzoffset = 0;
                                break;

                            case 3:
                                tzoffset = 5 * 60;
                                break;

                            case 4:
                                tzoffset = 4 * 60;
                                break;

                            case 5:
                                tzoffset = 6 * 60;
                                break;

                            case 6:
                                tzoffset = 5 * 60;
                                break;

                            case 7:
                                tzoffset = 7 * 60;
                                break;

                            case 8:
                                tzoffset = 6 * 60;
                                break;

                            case 9:
                                tzoffset = 8 * 60;
                                break;

                            case 10:
                                tzoffset = 7 * 60;
                                break;

                            default:
                                Context.CodeBug ();
                                break;

                        }
                    }
                }
            }
            if (year < 0 || mon < 0 || mday < 0)
                return double.NaN;
            if (sec < 0)
                sec = 0;
            if (min < 0)
                min = 0;
            if (hour < 0)
                hour = 0;

            double msec = date_msecFromDate (year, mon, mday, hour, min, sec, 0);
            if (tzoffset == -1) {
                /* no time zone specified, have to use local */
                return internalUTC (msec);
            }
            else {
                return msec + tzoffset * msPerMinute;
            }
        }

        private static string date_format (double t, int methodId)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder (60);
            double local = LocalTime (t);

            /* Tue Oct 31 09:41:40 GMT-0800 (PST) 2000 */
            /* Tue Oct 31 2000 */
            /* 09:41:40 GMT-0800 (PST) */

            if (methodId != Id_toTimeString) {
                appendWeekDayName (result, WeekDay (local));
                result.Append (' ');
                appendMonthName (result, MonthFromTime (local));
                result.Append (' ');
                append0PaddedUint (result, DateFromTime (local), 2);
                result.Append (' ');
                int year = YearFromTime (local);
                if (year < 0) {
                    result.Append ('-');
                    year = -year;
                }
                append0PaddedUint (result, year, 4);
                if (methodId != Id_toDateString)
                    result.Append (' ');
            }

            if (methodId != Id_toDateString) {
                append0PaddedUint (result, HourFromTime (local), 2);
                result.Append (':');
                append0PaddedUint (result, MinFromTime (local), 2);
                result.Append (':');
                append0PaddedUint (result, SecFromTime (local), 2);

                // offset from GMT in minutes.  The offset includes daylight
                // savings, if it applies.				
                int minutes = (int)Math.Floor ((LocalTZA + DaylightSavingTA (t)) / msPerMinute);
                // map 510 minutes to 0830 hours
                int offset = (minutes / 60) * 100 + minutes % 60;
                if (offset > 0) {
                    result.Append (" GMT+");
                }
                else {
                    result.Append (" GMT-");
                    offset = -offset;
                }
                append0PaddedUint (result, offset, 4);

                // Find an equivalent year before getting the timezone
                // comment.  See DaylightSavingTA.
                if (t < 0.0 || t > 2145916800000.0) {
                    int equiv = EquivalentYear (YearFromTime (local));
                    double day = MakeDay (equiv, MonthFromTime (t), DateFromTime (t));
                    t = MakeDate (day, TimeWithinDay (t));
                }
                result.Append (" (");
                DateTime date = FromMilliseconds (t);
                result.Append (date.ToString ("zzz"));
                result.Append (')');
            }
            return result.ToString ();
        }

        /* the javascript constructor */
        private static object jsConstructor (object [] args)
        {
            BuiltinDate obj = new BuiltinDate ();

            // if called as a constructor with no args,
            // return a new Date with the current time.
            if (args.Length == 0) {
                obj.date = now ();
                return obj;
            }

            // if called with just one arg -
            if (args.Length == 1) {
                object arg0 = args [0];
                if (arg0 is IScriptable)
                    arg0 = ((IScriptable)arg0).GetDefaultValue (null);
                double date;
                if (arg0 is string) {
                    // it's a string; parse it.
                    date = date_parseString ((string)arg0);
                }
                else {
                    // if it's not a string, use it as a millisecond date
                    date = ScriptConvert.ToNumber (arg0);
                }
                obj.date = TimeClip (date);
                return obj;
            }

            // multiple arguments; year, month, day etc.
            double [] array = new double [MAXARGS];
            int loop;
            double d;

            for (loop = 0; loop < MAXARGS; loop++) {
                if (loop < args.Length) {
                    d = ScriptConvert.ToNumber (args [loop]);

                    if (double.IsNaN (d) || System.Double.IsInfinity (d)) {
                        obj.date = double.NaN;
                        return obj;
                    }
                    array [loop] = ScriptConvert.ToInteger (args [loop]);
                }
                else {
                    array [loop] = 0;
                }
            }

            /* adjust 2-digit years into the 20th century */
            if (array [0] >= 0 && array [0] <= 99)
                array [0] += 1900;

            /* if we got a 0 for 'date' (which is out of range)
            * pretend it's a 1 */
            if (array [2] < 1)
                array [2] = 1;

            double day = MakeDay (array [0], array [1], array [2]);
            double time = MakeTime (array [3], array [4], array [5], array [6]);
            time = MakeDate (day, time);
            time = internalUTC (time);
            obj.date = TimeClip (time);

            return obj;
        }

        private static string toLocale_helper (double t, int methodId)
        {
            CultureInfo ci = Context.CurrentContext.CurrentCulture;

            switch (methodId) {
                case Id_toLocaleString:
                    DateTime date = FromMilliseconds (t);
                    return date.ToString (ci.DateTimeFormat.LongDatePattern)
                        + " " + date.ToString (ci.DateTimeFormat.LongTimePattern);

                case Id_toLocaleTimeString:
                    return FromMilliseconds (t).ToString (ci.DateTimeFormat.LongTimePattern);

                case Id_toLocaleDateString:
                    return FromMilliseconds (t).ToString (ci.DateTimeFormat.LongDatePattern);

            }
            throw Context.CodeBug ();
        }

        private static string js_toUTCString (double date)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder (60);

            appendWeekDayName (result, WeekDay (date));
            result.Append (", ");
            append0PaddedUint (result, DateFromTime (date), 2);
            result.Append (' ');
            appendMonthName (result, MonthFromTime (date));
            result.Append (' ');
            int year = YearFromTime (date);
            if (year < 0) {
                result.Append ('-');
                year = -year;
            }
            append0PaddedUint (result, year, 4);
            result.Append (' ');
            append0PaddedUint (result, HourFromTime (date), 2);
            result.Append (':');
            append0PaddedUint (result, MinFromTime (date), 2);
            result.Append (':');
            append0PaddedUint (result, SecFromTime (date), 2);
            result.Append (" GMT");
            return result.ToString ();
        }

        private static void append0PaddedUint (System.Text.StringBuilder sb, int i, int minWidth)
        {
            if (i < 0)
                Context.CodeBug ();
            int scale = 1;
            --minWidth;
            if (i >= 10) {
                if (i < 1000 * 1000 * 1000) {
                    for (; ; ) {
                        int newScale = scale * 10;
                        if (i < newScale) {
                            break;
                        }
                        --minWidth;
                        scale = newScale;
                    }
                }
                else {
                    // Separated case not to check against 10 * 10^9 overflow
                    minWidth -= 9;
                    scale = 1000 * 1000 * 1000;
                }
            }
            while (minWidth > 0) {
                sb.Append ('0');
                --minWidth;
            }
            while (scale != 1) {
                sb.Append ((char)('0' + (i / scale)));
                i %= scale;
                scale /= 10;
            }
            sb.Append ((char)('0' + i));
        }

        private static void appendMonthName (System.Text.StringBuilder sb, int index)
        {
            // Take advantage of the fact that all month abbreviations
            // have the same length to minimize amount of strings runtime has
            // to keep in memory
            string months = "Jan" + "Feb" + "Mar" + "Apr" + "May" + "Jun" + "Jul" + "Aug" + "Sep" + "Oct" + "Nov" + "Dec";
            index *= 3;
            for (int i = 0; i != 3; ++i) {
                sb.Append (months [index + i]);
            }
        }

        private static void appendWeekDayName (System.Text.StringBuilder sb, int index)
        {
            string days = "Sun" + "Mon" + "Tue" + "Wed" + "Thu" + "Fri" + "Sat";
            index *= 3;
            for (int i = 0; i != 3; ++i) {
                sb.Append (days [index + i]);
            }
        }

        private static double makeTime (double date, object [] args, int methodId)
        {
            int maxargs;
            bool local = true;
            switch (methodId) {

                case Id_setUTCMilliseconds:
                    local = false;
                    // fallthrough
                    goto case Id_setMilliseconds;

                case Id_setMilliseconds:
                    maxargs = 1;
                    break;


                case Id_setUTCSeconds:
                    local = false;
                    // fallthrough
                    goto case Id_setSeconds;

                case Id_setSeconds:
                    maxargs = 2;
                    break;


                case Id_setUTCMinutes:
                    local = false;
                    // fallthrough
                    goto case Id_setMinutes;

                case Id_setMinutes:
                    maxargs = 3;
                    break;


                case Id_setUTCHours:
                    local = false;
                    // fallthrough
                    goto case Id_setHours;

                case Id_setHours:
                    maxargs = 4;
                    break;


                default:
                    Context.CodeBug ();
                    maxargs = 0;
                    break;

            }

            int i;
            double [] conv = new double [4];
            double hour, min, sec, msec;
            double lorutime; /* Local or UTC version of date */

            double time;
            double result;

            /* just return NaN if the date is already NaN */
            if (double.IsNaN (date))
                return date;

            /* Satisfy the ECMA rule that if a function is called with
            * fewer arguments than the specified formal arguments, the
            * remaining arguments are set to undefined.  Seems like all
            * the Date.setWhatever functions in ECMA are only varargs
            * beyond the first argument; this should be set to undefined
            * if it's not given.  This means that "d = new Date();
            * d.setMilliseconds()" returns NaN.  Blech.
            */
            if (args.Length == 0)
                args = ScriptRuntime.padArguments (args, 1);

            for (i = 0; i < args.Length && i < maxargs; i++) {
                conv [i] = ScriptConvert.ToNumber (args [i]);

                // limit checks that happen in MakeTime in ECMA.
                if (conv [i] != conv [i] || System.Double.IsInfinity (conv [i])) {
                    return double.NaN;
                }
                conv [i] = ScriptConvert.ToInteger (conv [i]);
            }

            if (local)
                lorutime = LocalTime (date);
            else
                lorutime = date;

            i = 0;
            int stop = args.Length;

            if (maxargs >= 4 && i < stop)
                hour = conv [i++];
            else
                hour = HourFromTime (lorutime);

            if (maxargs >= 3 && i < stop)
                min = conv [i++];
            else
                min = MinFromTime (lorutime);

            if (maxargs >= 2 && i < stop)
                sec = conv [i++];
            else
                sec = SecFromTime (lorutime);

            if (maxargs >= 1 && i < stop)
                msec = conv [i++];
            else
                msec = msFromTime (lorutime);

            time = MakeTime (hour, min, sec, msec);
            result = MakeDate (Day (lorutime), time);

            if (local)
                result = internalUTC (result);
            date = TimeClip (result);

            return date;
        }

        private static double makeDate (double date, object [] args, int methodId)
        {
            int maxargs;
            bool local = true;
            switch (methodId) {

                case Id_setUTCDate:
                    local = false;
                    // fallthrough
                    goto case Id_setDate;

                case Id_setDate:
                    maxargs = 1;
                    break;


                case Id_setUTCMonth:
                    local = false;
                    // fallthrough
                    goto case Id_setMonth;

                case Id_setMonth:
                    maxargs = 2;
                    break;


                case Id_setUTCFullYear:
                    local = false;
                    // fallthrough
                    goto case Id_setFullYear;

                case Id_setFullYear:
                    maxargs = 3;
                    break;


                default:
                    Context.CodeBug ();
                    maxargs = 0;
                    break;

            }

            int i;
            double [] conv = new double [3];
            double year, month, day;
            double lorutime; /* local or UTC version of date */
            double result;

            /* See arg padding comment in makeTime.*/
            if (args.Length == 0)
                args = ScriptRuntime.padArguments (args, 1);

            for (i = 0; i < args.Length && i < maxargs; i++) {
                conv [i] = ScriptConvert.ToNumber (args [i]);

                // limit checks that happen in MakeDate in ECMA.
                if (conv [i] != conv [i] || System.Double.IsInfinity (conv [i])) {
                    return double.NaN;
                }
                conv [i] = ScriptConvert.ToInteger (conv [i]);
            }

            /* return NaN if date is NaN and we're not setting the year,
            * If we are, use 0 as the time. */
            if (double.IsNaN (date)) {
                if (args.Length < 3) {
                    return double.NaN;
                }
                else {
                    lorutime = 0;
                }
            }
            else {
                if (local)
                    lorutime = LocalTime (date);
                else
                    lorutime = date;
            }

            i = 0;
            int stop = args.Length;

            if (maxargs >= 3 && i < stop)
                year = conv [i++];
            else
                year = YearFromTime (lorutime);

            if (maxargs >= 2 && i < stop)
                month = conv [i++];
            else
                month = MonthFromTime (lorutime);

            if (maxargs >= 1 && i < stop)
                day = conv [i++];
            else
                day = DateFromTime (lorutime);

            day = MakeDay (year, month, day); /* day within year */
            result = MakeDate (day, TimeWithinDay (lorutime));

            if (local)
                result = internalUTC (result);

            date = TimeClip (result);

            return date;
        }

        #region PrototypeIds
        private const int ConstructorId_now = -3;
        private const int ConstructorId_parse = -2;
        private const int ConstructorId_UTC = -1;
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toTimeString = 3;
        private const int Id_toDateString = 4;
        private const int Id_toLocaleString = 5;
        private const int Id_toLocaleTimeString = 6;
        private const int Id_toLocaleDateString = 7;
        private const int Id_toUTCString = 8;
        private const int Id_toGMTString = 8;
        private const int Id_toSource = 9;
        private const int Id_valueOf = 10;
        private const int Id_getTime = 11;
        private const int Id_getYear = 12;
        private const int Id_getFullYear = 13;
        private const int Id_getUTCFullYear = 14;
        private const int Id_getMonth = 15;
        private const int Id_getUTCMonth = 16;
        private const int Id_getDate = 17;
        private const int Id_getUTCDate = 18;
        private const int Id_getDay = 19;
        private const int Id_getUTCDay = 20;
        private const int Id_getHours = 21;
        private const int Id_getUTCHours = 22;
        private const int Id_getMinutes = 23;
        private const int Id_getUTCMinutes = 24;
        private const int Id_getSeconds = 25;
        private const int Id_getUTCSeconds = 26;
        private const int Id_getMilliseconds = 27;
        private const int Id_getUTCMilliseconds = 28;
        private const int Id_getTimezoneOffset = 29;
        private const int Id_setTime = 30;
        private const int Id_setMilliseconds = 31;
        private const int Id_setUTCMilliseconds = 32;
        private const int Id_setSeconds = 33;
        private const int Id_setUTCSeconds = 34;
        private const int Id_setMinutes = 35;
        private const int Id_setUTCMinutes = 36;
        private const int Id_setHours = 37;
        private const int Id_setUTCHours = 38;
        private const int Id_setDate = 39;
        private const int Id_setUTCDate = 40;
        private const int Id_setMonth = 41;
        private const int Id_setUTCMonth = 42;
        private const int Id_setFullYear = 43;
        private const int Id_setUTCFullYear = 44;
        private const int Id_setYear = 45;
        private const int MAX_PROTOTYPE_ID = 45;
        #endregion

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length) {
                    case 6:
                        X = "getDay";
                        id = Id_getDay;
                        break;
                    case 7:
                        switch (s [3]) {
                            case 'D':
                                c = s [0];
                                if (c == 'g') { X = "getDate"; id = Id_getDate; }
                                else if (c == 's') { X = "setDate"; id = Id_setDate; }
                                break;
                            case 'T':
                                c = s [0];
                                if (c == 'g') { X = "getTime"; id = Id_getTime; }
                                else if (c == 's') { X = "setTime"; id = Id_setTime; }
                                break;
                            case 'Y':
                                c = s [0];
                                if (c == 'g') { X = "getYear"; id = Id_getYear; }
                                else if (c == 's') { X = "setYear"; id = Id_setYear; }
                                break;
                            case 'u':
                                X = "valueOf";
                                id = Id_valueOf;
                                break;
                        }
                        break;
                    case 8:
                        switch (s [3]) {
                            case 'H':
                                c = s [0];
                                if (c == 'g') { X = "getHours"; id = Id_getHours; }
                                else if (c == 's') { X = "setHours"; id = Id_setHours; }
                                break;
                            case 'M':
                                c = s [0];
                                if (c == 'g') { X = "getMonth"; id = Id_getMonth; }
                                else if (c == 's') { X = "setMonth"; id = Id_setMonth; }
                                break;
                            case 'o':
                                X = "toSource";
                                id = Id_toSource;
                                break;
                            case 't':
                                X = "toString";
                                id = Id_toString;
                                break;
                        }
                        break;
                    case 9:
                        X = "getUTCDay";
                        id = Id_getUTCDay;
                        break;
                    case 10:
                        c = s [3];
                        if (c == 'M') {
                            c = s [0];
                            if (c == 'g') { X = "getMinutes"; id = Id_getMinutes; }
                            else if (c == 's') { X = "setMinutes"; id = Id_setMinutes; }
                        }
                        else if (c == 'S') {
                            c = s [0];
                            if (c == 'g') { X = "getSeconds"; id = Id_getSeconds; }
                            else if (c == 's') { X = "setSeconds"; id = Id_setSeconds; }
                        }
                        else if (c == 'U') {
                            c = s [0];
                            if (c == 'g') { X = "getUTCDate"; id = Id_getUTCDate; }
                            else if (c == 's') { X = "setUTCDate"; id = Id_setUTCDate; }
                        }
                        break;
                    case 11:
                        switch (s [3]) {
                            case 'F':
                                c = s [0];
                                if (c == 'g') { X = "getFullYear"; id = Id_getFullYear; }
                                else if (c == 's') { X = "setFullYear"; id = Id_setFullYear; }
                                break;
                            case 'M':
                                X = "toGMTString";
                                id = Id_toGMTString;
                                break;
                            case 'T':
                                X = "toUTCString";
                                id = Id_toUTCString;
                                break;
                            case 'U':
                                c = s [0];
                                if (c == 'g') {
                                    c = s [9];
                                    if (c == 'r') { X = "getUTCHours"; id = Id_getUTCHours; }
                                    else if (c == 't') { X = "getUTCMonth"; id = Id_getUTCMonth; }
                                }
                                else if (c == 's') {
                                    c = s [9];
                                    if (c == 'r') { X = "setUTCHours"; id = Id_setUTCHours; }
                                    else if (c == 't') { X = "setUTCMonth"; id = Id_setUTCMonth; }
                                }
                                break;
                            case 's':
                                X = "constructor";
                                id = Id_constructor;
                                break;
                        }
                        break;
                    case 12:
                        c = s [2];
                        if (c == 'D') { X = "toDateString"; id = Id_toDateString; }
                        else if (c == 'T') { X = "toTimeString"; id = Id_toTimeString; }
                        break;
                    case 13:
                        c = s [0];
                        if (c == 'g') {
                            c = s [6];
                            if (c == 'M') { X = "getUTCMinutes"; id = Id_getUTCMinutes; }
                            else if (c == 'S') { X = "getUTCSeconds"; id = Id_getUTCSeconds; }
                        }
                        else if (c == 's') {
                            c = s [6];
                            if (c == 'M') { X = "setUTCMinutes"; id = Id_setUTCMinutes; }
                            else if (c == 'S') { X = "setUTCSeconds"; id = Id_setUTCSeconds; }
                        }
                        break;
                    case 14:
                        c = s [0];
                        if (c == 'g') { X = "getUTCFullYear"; id = Id_getUTCFullYear; }
                        else if (c == 's') { X = "setUTCFullYear"; id = Id_setUTCFullYear; }
                        else if (c == 't') { X = "toLocaleString"; id = Id_toLocaleString; }
                        break;
                    case 15:
                        c = s [0];
                        if (c == 'g') { X = "getMilliseconds"; id = Id_getMilliseconds; }
                        else if (c == 's') { X = "setMilliseconds"; id = Id_setMilliseconds; }
                        break;
                    case 17:
                        X = "getTimezoneOffset";
                        id = Id_getTimezoneOffset;
                        break;
                    case 18:
                        c = s [0];
                        if (c == 'g') { X = "getUTCMilliseconds"; id = Id_getUTCMilliseconds; }
                        else if (c == 's') { X = "setUTCMilliseconds"; id = Id_setUTCMilliseconds; }
                        else if (c == 't') {
                            c = s [8];
                            if (c == 'D') { X = "toLocaleDateString"; id = Id_toLocaleDateString; }
                            else if (c == 'T') { X = "toLocaleTimeString"; id = Id_toLocaleTimeString; }
                        }
                        break;
                }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion
            return id;
        }



        /* cached values */
        private static System.TimeZone thisTimeZone;
        private static double LocalTZA;

        private double date;

        private static readonly DateTime StandardBaseTime = new DateTime (1970, 1, 1, 0, 0, 0, 0);
        /// <summary>
        /// Allocates a Date object and initializes it to represent the specified
        /// number of milliseconds since the standard base time known as
        /// "the epoch", namely January 1, 1970, 00:00:00 GMT.
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>		
        internal static DateTime FromMilliseconds (double ms)
        {
            return StandardBaseTime.AddMilliseconds (ms);
        }

    }
}
