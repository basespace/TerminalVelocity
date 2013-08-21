﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Illumina.TerminalVelocity.Tests
{
    public class Constants
    {
        public const string ONE_GIG_FILE_S_SL = @"https://1000genomes.s3.amazonaws.com/release/20110521/ALL.wgs.phase1_release_v3.20101123.snps_indels_sv.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425600785&Signature=KQ3qGSqFYN0z%2BHMTGLAGLUejtBw%3D";
        public const string ONE_GIG_FILE = @"http://1000genomes.s3.amazonaws.com/release/20110521/ALL.wgs.phase1_release_v3.20101123.snps_indels_sv.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425600785&Signature=KQ3qGSqFYN0z%2BHMTGLAGLUejtBw%3D";
        public const string ONE_GIG_REDIRECT = @"http://tinyurl.com/m3x2kz7";
        public const long ONE_GIG_FILE_LENGTH = 1297662912;
        public const string ONE_GIG_CHECKSUM = "24b9f9d41755b841eaf8d0faeab00a6c";//24b9f9d41755b841eaf8d0faeab00a6c
        public const string TWENTY_CHECKSUM = "11db70c5bd445c4b41de6cde9d655ee8";

        public const string TWENTY_MEG_FILE =
            @"https://1000genomes.s3.amazonaws.com/release/20100804/ALL.chrX.BI_Beagle.20100804.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425620139&Signature=h%2BIqHbo2%2Bjk0jIbR2qKpE3iS8ts%3D";

        public const string THIRTY_GIG_FILE = @"https://1000genomes.s3.amazonaws.com/data/HG02484/sequence_read/SRR404082_2.filt.fastq.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1362529020&Signature=l%2BS3sA1vkZeFqlZ7lD5HrQmY5is%3D";
        public const int TWENTY_MEG_FILE_LENGTH = 29996532;
    }
}
