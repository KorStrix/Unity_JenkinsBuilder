#!/bin/bash

WORKSPACE=$1

cd "$WORKSPACE"
echo "Determine BUILD Version in Unity Project"
source "${WORKSPACE}/Assets/Editor/Build/TechSupport/export_build_ver.sh"

TARGET_IPA_FILE_NAME="${ProjectName}_${BUILD_VER}.ipa"
XCODE_PROJECT_DIR="${WORKSPACE}/Build/build"
IPA_EXPORT_PATH="${XCODE_PROJECT_DIR}/.."

echo "Export from ARCHIVE to IPA"

xcodebuild -allowProvisioningUpdates \
           -exportArchive \
           -exportOptionsPlist "${XCODE_PROJECT_DIR}/exportOptions.plist" \
           -exportPath "${IPA_EXPORT_PATH}" \
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${BUILD_VER}.xcarchive"