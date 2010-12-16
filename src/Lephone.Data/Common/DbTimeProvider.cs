﻿using System;
using Lephone.Core;

namespace Lephone.Data.Common
{
    public class DbTimeProvider
    {
        private readonly DbContext _context;
        private DateTime _lastCheckTime;
        private TimeSpan _timeDiff;

        public DbTimeProvider(DbContext context)
        {
            _context = context;
        }

        public DateTime Now
        {
            get
            {
                var now = MiscProvider.Instance.Now;
                if((now - _lastCheckTime).TotalMinutes > DataSettings.DbTimeCheckMinutes)
                {
                    _lastCheckTime = now;
                    var dbNow = _context.GetDatabaseTime();
                    _timeDiff = now - dbNow;
                }
                return now - _timeDiff;
            }
        }
    }
}