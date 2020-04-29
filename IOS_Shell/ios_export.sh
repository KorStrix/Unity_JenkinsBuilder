#!/bin/bash

WORKSPACE=$1

cd "$WORKSPACE"

ProjectName="KH"
BUILD_VER="1"

TARGET_IPA_FILE_NAME="${ProjectName}_${BUILD_VER}.ipa"
# XCODE_PROJECT_DIR="${WORKSPACE}/${BUILD_VER}/${ProjectName}_xcode"
XCODE_PROJECT_DIR="Build"
IPA_EXPORT_PATH="${XCODE_PROJECT_DIR}/.."

echo "BUILD VER is [$BUILD_VER]"
echo "TARGET IPA FILE NAME is [$TARGET_IPA_FILE_NAME]"

echo "XCODE_PROJECT_DIR=${XCODE_PROJECT_DIR}"
cd "$XCODE_PROJECT_DIR"
echo "Archive iOS BUILD"

if test -d "${XCODE_PROJECT_DIR}/Unity-iPhone.xcworkspace"
then
echo "xcodebuild with xcode workspace"
xcodebuild DEVELOPMENT_TEAM="${AppleTeamID}" \
           -allowProvisioningUpdates \
           -workspace Unity-iPhone.xcworkspace \
           -scheme Unity-iPhone \
           -configuration Release archive \
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${BUILD_VER}"
else
echo "xcodebuild with xcode project"
xcodebuild DEVELOPMENT_TEAM="${AppleTeamID}" \
           -allowProvisioningUpdates \
           -scheme Unity-iPhone \
           -configuration Release archive \
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${BUILD_VER}"
fi

echo "Export from ARCHIVE to IPA"

xcodebuild -allowProvisioningUpdates \
           -exportArchive \
           -exportOptionsPlist "${XCODE_PROJECT_DIR}/exportOptions.plist" \
           -exportPath "${IPA_EXPORT_PATH}" \
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${BUILD_VER}.xcarchive"