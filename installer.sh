#!/usr/bin/env bash

# Author: Unity Technologies
#
#---------------------------------------------------------------
# Purpose
#---------------------------------------------------------------
#
# This shell installer allows you to install and uninstall the
# UGS CLI.
#
# The unity gaming services (UGS) CLI is a unified command line
# interface tool for gaming services.
#
#---------------------------------------------------------------
# Usage
#---------------------------------------------------------------
#
# To install the latest version of the UGS CLI, call
#
#     curl -s PLACEHOLDER | bash
#
# from your command line.
#
# To install a specific version of the UGS CLI, you can add a
# version parameter like so:
#
#     curl -s PLACEHOLDER | version=<version> bash
#
#     example: curl -s PLACEHOLDER | version=1.0.0-beta.4 bash
#
# To uninstall the UGS CLI:
#
#     curl -s PLACEHOLDER | uninstall=true bash
#
#---------------------------------------------------------------
# Arguments
#---------------------------------------------------------------
#
# version=<version>
# description: When added, it allows you to specify the version
#              of the UGS CLI that you want to install
# default:     latest
# example:     version=1.0.0-beta.4
#
# uninstall=<bool>
# description: When set to true, it allows you to uninstall the UGS CLI
# default:     false
# example:     uninstall=true
#
#---------------------------------------------------------------
# Useful Links
#---------------------------------------------------------------
#
# Documentation:
# https://services.docs.unity.com/guides/ugs-cli/latest/general/overview
#
# GitHub repo:
# https://github.com/Unity-Technologies/unity-gaming-services-cli
#
# License:
# https://github.com/Unity-Technologies/unity-gaming-services-cli/blob/main/License.md
#
# Support:
# https://support.unity.com/hc/en-us/requests/new?ticket_form_id=360001936712&serviceName=cli
#

# GitHub repository metadata
ORGANIZATION="Unity-Technologies"
REPO_NAME="unity-gaming-services-cli"

# Command line echo utilities
INFORMATION_TAG="[Information]"
WARNING_TAG="\033[33m[Warning]\033[0m"
ERROR_TAG="\033[31m[Error]\033[0m"
SUCCESS_TAG="\033[32m[Success]\033[0m"
TAB=">   "

INSTALL_DIRECTORY="/usr/local/bin"
SUPPORT_URL="https://support.unity.com/hc/en-us/requests/new?ticket_form_id=360001936712&serviceName=cli"
UGS_EXISTS=$(which ugs)

# This section manages the uninstallation of the CLI when a user specifies
# uninstall=true in the command line. If all checks pass, we proceed to uninstall the CLI.
if [[ ! -z $UGS_EXISTS ]]
then
    NPM_UGS_EXISTS=$(npm list -g ugs > /dev/null 2>&1; echo $?)

    if [[ $uninstall == "true" ]]
    then
        if [[ $NPM_UGS_EXISTS == 0 ]]
        then
            echo -e "$ERROR_TAG Cannot uninstall the UGS CLI."
            echo -e "$TAB Your version of UGS was installed with npm."
            echo -e "$TAB Try uninstalling it using 'npm uninstall -g ugs'."
            exit 1
        else
            echo -e "$INFORMATION_TAG Starting uninstallation"
            echo -e "$TAB Removing binaries..."
            sudo rm $UGS_EXISTS
            echo -e "$TAB Binaries removed."
            echo ""
            echo -e "$SUCCESS_TAG ugs uninstalled"
            exit 0
        fi
    fi
elif [[ -z $UGS_EXISTS && $uninstall == "true" ]]
then
    echo -e "$ERROR_TAG Cannot uninstall the UGS CLI."
    echo -e "$TAB Could not find ugs on your system."
    exit 1
fi

# Small check to see if it's possible to install the binaries. Will prompt an error if ugs already exists.
if [[ ! -z $UGS_EXISTS ]]
then
    if [[ $NPM_UGS_EXISTS == 0 ]]
    then
        echo ""
        echo -e "$ERROR_TAG Cannot install the UGS CLI."
        echo -e "$TAB A version of the UGS CLI already exist and was installed with npm."
        echo -e "$TAB Try uninstalling it using 'npm uninstall -g ugs'."
        exit 1
    else
        echo ""
        echo -e "$ERROR_TAG Cannot install the UGS CLI."
        echo -e "$TAB The UGS CLI already exists at '$UGS_EXISTS'"
        echo -e "$TAB Remove that version and try again."
        exit 1
    fi
fi

# We start gathering basic information to determine the GitHub download link
echo -e "$INFORMATION_TAG Assembling download link"
echo -e "$TAB Verifying operating system type..."

# Get operating system type
OPERATING_SYSTEM=$(uname -s | tr '[:upper:]' '[:lower:]')

# Rename darwin to macos for clarity
if [[ $OPERATING_SYSTEM == "darwin" ]]
then
    OPERATING_SYSTEM="macos"
fi

echo -e "$TAB Operating system detected: $OPERATING_SYSTEM"

# Verify operating system support
if [[ $OPERATING_SYSTEM == "macos" || $OPERATING_SYSTEM == "linux" ]]
then
    echo -e "$TAB Your operating system '$OPERATING_SYSTEM' is supported."
else
    echo ""
    echo -e "$ERROR_TAG Your operating system '$OPERATING_SYSTEM' is not supported."
    echo -e "$TAB Currently supported operating systems for this bash installer are Linux and MacOS"
    echo -e "$TAB If your operating system is Linux or MacOS, open a ticket here:"
    echo -e "$TAB $SUPPORT_URL"
    exit 1
fi

# Determine the release tag that will be used to fetch the release
if [[ ! -z $version ]]
then
    RELEASE_TAG="v$version"
    echo -e "$TAB Option 'version' specified with value '$version'."
    echo -e "$TAB Checking if version exists on GitHub..."
    response=$(curl -s "https://api.github.com/repos/$ORGANIZATION/$REPO_NAME/releases/tags/$RELEASE_TAG")

    if [[ "$response" != *"tag_name"* ]]
    then
        echo ""
        echo -e "$ERROR_TAG Release version $version does not exist."
        echo -e "$TAB The release version specified does not exist."
        echo -e "$TAB To see a list of all the released versions of the UGS CLI, visit:"
        echo -e "$TAB https://github.com/Unity-Technologies/unity-gaming-services-cli/releases"
        exit 1
    else
        echo -e "$TAB Release version $version exists."
    fi
else
    echo -e "$TAB No 'version' option specified, getting latest version from GitHub..."
    RELEASE_TAG=$(curl -s "https://api.github.com/repos/$ORGANIZATION/$REPO_NAME/releases/latest" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')

    if [[ -z RELEASE_TAG ]]
    then
        echo ""
        echo -e "$ERROR_TAG Could not determine latest release tag"
        echo -e "$TAB This could be due to network issues, or reaching the api request limit on GitHub."
        echo -e "$TAB Try again later or file a support ticket if the problem persists:"
        echo -e "$TAB $SUPPORT_URL"
        exit 1
    fi

    echo -e "$TAB Found latest version: $RELEASE_TAG"
fi

ASSET_NAME="ugs-$OPERATING_SYSTEM-x64"
GITHUB_API_URL="https://github.com/$ORGANIZATION/$REPO_NAME/releases/download/$RELEASE_TAG/$ASSET_NAME"
echo -e "$TAB GitHub download link: $GITHUB_API_URL"

# If we reach this point, all checks have passed. We have all the information to download
# and install the UGS CLI.
#
# Download UGS CLI to /usr/local/bin
echo ""
echo -e "$INFORMATION_TAG Installing the UGS CLI"
echo -e "$TAB Downloading binaries to $INSTALL_DIRECTORY..."
sudo mkdir -p "$INSTALL_DIRECTORY"
sudo curl -o "$INSTALL_DIRECTORY/ugs" -L --progress-bar $GITHUB_API_URL

# Use chmod +rx on the binaries to mark them as executable
echo -e "$TAB Marking binaries as executable..."
sudo chmod +rx "$INSTALL_DIRECTORY/ugs"

# We're done, nice! All that's left is a check to see if the executable is in PATH
echo -e "$TAB All done."
echo ""
echo -e "$SUCCESS_TAG Installation completed"

UGS_VERSION=$(ugs --version > /dev/null 2>&1; echo $?)

# Check if the executable is in PATH
if [[ ! ":$PATH:" == *":$INSTALL_DIRECTORY:"* ]];
then
    echo -e "$WARNING_TAG UGS CLI was installed correctly, but could not be automatically added to your PATH."
    echo -e "$TAB To be able to call ugs, add $INSTALL_DIRECTORY to your PATH by modifying ~/.profile or ~/.bash_profile, then reopen your command line."
else
    echo -e "$TAB To get started, type 'ugs -h'."
fi
