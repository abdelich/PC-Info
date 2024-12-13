# PC Information Collector

A portable application for collecting and displaying detailed information about a computer's hardware, software, user accounts, and network settings. The application supports exporting the gathered data to TXT or PDF formats and sending it via email. It is designed to run autonomously without requiring installation.

---

## Features

### Core Functionality
- **Comprehensive Data Collection**: Collects detailed information about both hardware and software components of the computer.
- **User Data Gathering**: Retrieves user account details, including passwords (if accessible).
- **Export Options**: Exports collected data in TXT or PDF formats.

### Collected Information

#### **Hardware**
- **HDD/SSD/NVMe**:
  - Type (HDD, SSD, NVMe)
  - Size
  - Manufacturer and model
  - List of all disks
- **Graphics Card**:
  - Manufacturer and model
  - List of all installed GPUs
- **Computer**:
  - Model and manufacturer of the device
- **CPU**:
  - Name and model of the processor
- **RAM**:
  - Number of memory sticks
  - Manufacturer of each stick
  - Capacity of each stick
  - Total memory capacity

#### **Software**
- **Operating System**:
  - Version of Windows
  - Architecture (x86/x64)
  - License type (Retail, OEM, Volume)
  - Serial number (if accessible)
- **Office Applications**:
  - License type (Retail, Volume, Subscription)
  - Version of Office
  - Serial number (if accessible)
  - Information about each installed Office instance
- **Outlook**:
  - Version of Outlook
  - List of all email accounts (login and email address)
  - Email account passwords (if accessible)
  - File storage paths (OST/PST) with their sizes and full paths

#### **Network**
- **General Network Data**:
  - Domain name (if in a domain)
  - Workgroup name (if not in a domain)
  - Computer name
- **Network Cards**:
  - Manufacturer and model of each network card
  - IP addresses
  - MAC addresses

#### **Users**
- Names of all user accounts
- Passwords (if accessible)

### Customization
- **Interface Customization**:
  - Add your company logo
  - Customize color scheme, fonts
- **Export Formats**:
  - Choose between TXT and PDF for data export

### Portable Version
- Fully standalone and does not require installation.

---

## System Requirements

### Supported Operating Systems
- **Client Systems**:
  - Windows 7, 8, 8.1, 10, 11
- **Server Systems**:
  - Windows Server 2008 R2 and later

### Development Languages
- C# + WPF
- MVVM Architecture

---

## Installation

No installation required. Simply download the executable and run it.

---

## Usage

1. **Launch the application**: Double-click the executable file.
2. **View collected data**: The application displays information about availiable export information.
3. **Export data**:
   - Click on the "Export" button.
   - Choose the desired format (TXT or PDF).
   - Save the file to the desired location.
4. **Send data via email**:
   - Chose Email in format
   - Enter the recipient's email address.
   - Click on "Send".
6. **Customize the interface**:
   - Add your logo and adjust styles via the settings menu.
# PC-Info
