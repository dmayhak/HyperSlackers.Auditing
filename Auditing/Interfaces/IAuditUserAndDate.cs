﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework
{
    public interface IAuditUserAndDate<TKey> : IAuditable<TKey>
    {
        DateTime CreatedDate { get; set; }
        TKey CreatedBy { get; set; }
        DateTime LastChangedDate { get; set; }
        TKey LastChangedBy { get; set; }
    }
}
