using System;
using System.Collections.Generic;
using System.Text;

namespace FlashGamer
{
    public class Template
    {
        //small Room events template
        public readonly struct userREjoin
        {
            //meaning in normal room event that involves names only, it is seperated by '*'.
            const char oneID = '*';
        }

        public readonly struct userRELeave
        {
            //meaning in normal room event that involves names only, it is seperated by '*'.
            const char oneID = '*';
        }

        public readonly struct userREKicked
        {
            //meaning in normal room event that involves names only, it is seperated by '*'.
            const char oneID = '*';
        }

        public readonly struct userREtext
        {
            //meaing one (first) is id and second is message (the text) seperated by '*' and text
            //EOL is ':`9'
            const char oneID = '*';
            const string twoMsg = ":`9";
        }

        public readonly struct userRERejected
        {
            const char oneInvokerID = '*';
        }

        /*
                 REText = 51,
        REVoice = 52,
        REJoinSmall = 53,
        REJoin = 54,
        RELeaveSmall = 55,
        RELeave = 56,
        REKick = 57,
        REKicked = 58,
        REInvite = 59,
        REReject = 60,*/
    }
}
