# Inkbook Data Extractor
[![Windows Universal](https://github.com/tattoomachinegirl/inkbook-dataextractor/actions/workflows/build.yml/badge.svg)](https://github.com/tattoomachinegirl/inkbook-dataextractor/actions/workflows/build.yml)

## Purpose
This tool is designed to extract client and appointments for a given employee from inkbook backup files 
although this is designed for ink book ,  it should theoretically work with other products from Daysmart Software  including 

- 123Pet 
- Salon Iris
- Orchid
- Inkbook

This app was designed for exporting data into other applications for reporting, integration, ETL and employee offboarding  purposes 
I am not affiliated with daysmart and software is provided as is 

## Easy Mode

To Run this tool the easiest way is to follow this guide 

use the following steps 

Download the latest zip package from the [Releases page](https://github.com/tattoomachinegirl/inkbook-dataextractor/releases) 

Extract to any directory you wish to work from ,  this will be your working directory for data extraction 


Go to Options 


![image](https://user-images.githubusercontent.com/452012/122485029-f8757c00-cfa3-11eb-8de5-de652c73ea25.png)


Download Backup

![image](https://user-images.githubusercontent.com/452012/122484923-bc421b80-cfa3-11eb-8d70-8c34c5a3c8aa.png)


Place downloaaded file in the directory you extracted this tool into 

Rename the file data.xml 
(an example backup file is included, replace or delete this file)

Run extract.exe 

A command window will pop up 
this will read the employee table from the file 
and give you a list to choose from 
![image](https://user-images.githubusercontent.com/452012/122485594-56ef2a00-cfa5-11eb-9220-db313d75362b.png)

enter the corresponding employeeid you would like to extract data for and press enter 

![image](https://user-images.githubusercontent.com/452012/122485790-bf3e0b80-cfa5-11eb-931d-e7c355d30eb8.png)

the export folder should open automatically in windows explorer 
![image](https://user-images.githubusercontent.com/452012/122486203-94a08280-cfa6-11eb-9965-1e725fb9c10b.png)

review and verify the data in excel or any text editor 
![image](https://user-images.githubusercontent.com/452012/122486363-eea14800-cfa6-11eb-817b-c2c2f33bc9a0.png)

Import into your favourite database or CRM 


## Advanced 

The following options are provide if you wish to run this from the commandd line 

```
Usage:
  extract [options]

Options:
  --file <file>                                               path to backup file
  --log-directory <log-directory>                             logDirectory [default: ./logs ]
  --output <output>                                           output [default: ./output ]
  --loglevel <Debug|Error|Fatal|Information|Verbose|Warning>  loglevel [default: Information]
  --version                                                   Show version information
  -?, -h, --help                                              Show help and usage information
```


