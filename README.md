# WA DMS Licence Finder

A .NET console application that matches licence records between NALD (National Activities Licensing Database)
extracts and DMS (Document Management System) extracts using configurable matching rules.

## Overview

The WA DMS Licence Finder processes licence data from multiple sources to:
- Match NALD licence records with corresponding DMS document records
- Apply multiple matching rules with priority-based execution
- Generate detailed Excel reports showing match results and statistics
- Compare results with previous iterations to track changes
- Handle manual fixes and corrections through separate extract files

## Features

- **Multi-Rule Matching Engine**: Uses priority-based rules to find the best matches between NALD and DMS records
- **Excel Processing**: Reads from multiple Excel files and generates comprehensive output reports
- **Iteration Comparison**: Compares current results with previous runs to identify changes
- **Manual Fix Support**: Processes manual correction files to override automatic matching
- **Progress Tracking**: Real-time console output showing processing progress
- **Comprehensive Reporting**: Detailed match results with metadata and statistics

## Resource Files Required

The application expects the following Excel files to be placed in a `Resources` folder within the application directory:

### Required Input Files

1. **DMS Extract Files**
   - **Pattern**: XLSX files starting with `Site`
   - **Required columns**:
     - `Permit Number`
     - `Document Date`
     - `Uploaded Date`
     - `File URL`
     - `File Name`
     - `File Size`
     - `Disclosure Status`
     - `Other Reference`
     - `Modified Date`
     - `File ID`

2. **NALD Extract Files**
   - **Pattern**: XLSX files starting with `NALD_Extract`
   - **Required columns**:
     - `Licence No.`
     - `Region`

3. **Manual Fix Files** (Optional)
    - **Pattern**: XLSX files starting with `Manual_Fix_Extract`
    - **Required columns**:
        - `DMS Version Of Licence No.`
        - `DMS Permit Folder No.`

4. **Previous Iteration Files** (Optional)
   - **Pattern**: XLSX files starting with `Previous_Iteration_Matches`
   - **Required columns**: Same as output format (see below)

5. **NALD_Metadata** (Optional)

6. **NALD_Metadata_Reference** (Optional)

7. **Overrides** (Optional)

8. **File_Reader_Extract** (Optional)

9. **Template_Results** (Optional)

10. **File_Identification_Extract** (Optional)

### Resource Folder Structure