#!/bin/bash
# IOS Uploader Bash입니다.
# 주의사항
# Window에서 파일 수정후 맥에서 실행시 줄바꿈 코드(EOL)를 Unix 형식으로 변경바랍니다. (Window/MAC은 안됨)

WORKSPACE=$1
AppleTeamID=$2
ProjectName=$3

cd "$WORKSPACE"

XCODE_PROJECT_DIR="Build"
IPA_EXPORT_PATH="${XCODE_PROJECT_DIR}/.."


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
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${ProjectName}"
else
echo "xcodebuild with xcode project"
xcodebuild DEVELOPMENT_TEAM="${AppleTeamID}" \
           -allowProvisioningUpdates \
           -scheme Unity-iPhone \
           -configuration Release archive \
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${ProjectName}"
fi

echo "Export from ARCHIVE to IPA"

xcodebuild -allowProvisioningUpdates \
           -exportArchive \
           -exportOptionsPlist "exportOptions.plist" \
           -exportPath "${IPA_EXPORT_PATH}" \
           -archivePath "${XCODE_PROJECT_DIR}/../archive/${ProjectName}.xcarchive"