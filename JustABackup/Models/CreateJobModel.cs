﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace JustABackup.Models
{
    public class CreateJobModel : BaseViewModel
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }

        [Required]
        public string CronSchedule { get; set; }

        [Required]
        public int StorageProvider { get; set; }

        [Required]
        public int BackupProvider { get; set; }

        public SelectList StorageProviders { get; set; }
        public SelectList BackupProviders { get; set; }
    }
}
