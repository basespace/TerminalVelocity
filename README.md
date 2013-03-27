TerminalVelocity
================

Purpose of this project is to download a large file in .net as fast as possible. Think of curl, but only for downloading large files multi-threaded and as fast as possible.

 This specifically only works with file download over http/https with servers that [accept byte range requests](http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html). Specifically we only support the Range: bytes={start}-{end} header.  Even more specific, this has mainly been used and tested against Amazon S3's http endpoints.

# Install #

- Delivered as a dll or a console application (Nuget coming soon).  
- Requires .NET 4 + and has no external dependencies.

# Features #


- Custom TCP client written specifically to do range requests
- Works over http or https
- Can handle network glitches
- Has built-in throttling to prevent excessive bandwidth use and high memory consumption
- Settings to tweak thread and chunk size
- Can output to any stream
- Works on .net and mono
- Can detect redirect/shortened urls and download originating location

# Command Line Usage #

- Download data from 1000 genomes

`TerminalVelocity.exe https://1000genomes.s3.amazonaws.com/data/HG00114/alignment/HG00114.chrom11.ILLUMINA.bwa.GBR.low_coverage.20111114.bam?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1427423604&Signature=z8jczjwn436BxOaM5nLoVhaqD%2Fs%3D`

- Download data from shortened url of same address

`TerminalVelocity.exe http://tinyurl.com/cynt8ht`


- Download 1GB+ file (MD5 290f8099861e8089cec020508a57d2b2)

`TerminalVelocity.exe https://1000genomes.s3.amazonaws.com/release/20110521/ALL.wgs.phase1_release_v3.20101123.snps_indels_sv.sites.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1425600785&Signature=KQ3qGSqFYN0z%2BHMTGLAGLUejtBw%3D`

- Download a file and choose thread limit to 2 and max chunk to 5000

`TerminalVelocity.exe http://tinyurl.com/cynt8ht --mt=2 --mc=5000`

- Set the file output

`TerminalVelocity.exe http://tinyurl.com/cynt8ht --f="C:\github\TerminalVelocity\src\TerminalVelocity.Console\bin\Debug\helloworld.txt" '







