#!/bin/bash
# 주의사항
# Window에서 파일 수정후 맥에서 실행시 줄바꿈 코드(EOL)를 Unix 형식으로 변경바랍니다. (Window/MAC은 안됨)

APP_STORE_USERNAME=$1
APP_STORE_PASSWORD=$2
IPA_PATH=$3

echo "Validate App IPA_PATH : $IPA_PATH"
xcrun altool --validate-app --file "$IPA_PATH" --username "$APP_STORE_USERNAME" --password "$APP_STORE_PASSWORD"

echo "Upload App IPA_PATH : $IPA_PATH"
xcrun altool --upload-app --file "$IPA_PATH" --username "$APP_STORE_USERNAME" --password "$APP_STORE_PASSWORD"