# Unity_JenkinsBuilder
젠킨스에서 실행하는 유니티 빌드 스크립트 에셋입니다.

정확하게는 외부에서 커맨드라인을 통해 원하는 빌드를 뽑는 툴이며 편의상 **Jenkins Builder**로 칭합니다.


# 1. 설치 방법
[링크](https://github.com/KorStrix/Unity_DevelopmentDocs/blob/master/GitHub/UnityPackage.md)를 참고바랍니다.

## 해당 프로젝트에 정상 설치되었는지 확인 방법
**Unity Editor 상단탭/Tools/Build에 메뉴가 뜨면 성공!**

---
# 2. 빌드 테스트

## 2-1. Build Config 생성 및 Editor에서 바로 빌드 테스트
1. 유니티 에디터의 상단 탭 - Tools/Build/Create Build Config Exmaple File을 생성합니다.
2. Build Config는 Json 형식으로 이루어져 있으며, 각 항목당 설명은 스크립트에 주석[(링크)](https://github.com/KorStrix/Unity_JenkinsBuilder/blob/master/Editor/BuildConfig.cs)으로 달았습니다.
이를 참고하며 Build Config 파일을 프로젝트에 맞게 수정합니다.
3. 수정 완료 후 Tools/Build/Show Jenkins Builder Window를 통해 윈도우를 엽니다.
4. 아까 작성한 Config File Path를 설정 후 빌드 파일을 어디에 생성할지 세팅합니다.
5. Android or IOS Build를 합니다.
* 빌드 하기 전 주의사항 ) 프로젝트의 Platform이 해당 플랫폼인지 확인합니다.
* AOS는 APK를 바로 빌드할 수 있지만, IOS의 경우 유니티에선 Xcode Project로 Export밖에 못합니다.

6. **APK or XCode Project가 세팅한 Output path에 나오면 성공!**

---
## 2-2. 젠킨스 - 안드로이드 빌드 테스트

안드로이드는 크게 어렵지 않기 때문에 자세한 설명을 생략합니다.

### 빌드 방법

1. 젠킨스 프로젝트를 생성 합니다.
*. 젠킨스 프로젝트 생성 관련 내용은 여기서 다루지 않습니다.
2. 젠킨스 프로젝트 구성 - Build 항목에 Invoke Unity3D Editor 항목이 있습니다. 여기서 Editor command line arguments를 수정할 예정입니다.
* 이 항목에는 Unity Default CommandLine과 이 프로젝트의 CommandLine을 같이 쓰며 CommandLine을 통해 **"어떻게"** 유니티를 실행시키고, 이 프로젝트의 빌드 스크립트를 **"어떻게"** 실행하는지를 세팅하는 곳입니다.
* 유니티 에디터의 커맨드라인 중 -quit -batchmode -logFile log.txt를 넣습니다. 이에 대한 설명은 유니티 메뉴얼[(링크)](https://docs.unity3d.com/kr/530/Manual/CommandLineArguments.html)를 참고합니다.
* 하단의 라인도 연달아 삽입합니다.
```
-executeMethod Jenkins.Builder.Build_Android -config_path YOUR_CONFIG_PATH.json -output_path ${JENKINS_HOME}/jobs/${JOB_NAME}/builds/${BUILD_NUMBER}/archive
```
여기서 YOUR_CONFIG_PATH에 Build Config 파일의 경로를 세팅합니다.
다른 커맨드 라인을 보고싶으시면 스크립트[(링크)](https://github.com/KorStrix/Unity_JenkinsBuilder/blob/master/Editor/JenkinsBuilder.cs)를 참고바랍니다.

- ${JENKINS_HOME}, ${JOB_NAME}, ${BUILD_NUMBER}의 경우 Jenkins 환경변수입니다.


3. **Build를 한 뒤 Archive에 APK파일이 있으면 성공!**

---
## 2-3. 젠킨스 - IOS 빌드 테스트

### 주의사항
**IOS 빌드를 위해선 MAC OS의 PC와 Apple 개발자 계정이 필요합니다.**

### 개요
안드로이드의 경우 PC -> APK 추출 -> 선택에 따라 -> APK 스토어 업로드까지 크게 어렵지는 않으나,

IOS의 경우 ipa를 공유하려면 
PC -> XCode Project -> ipa 추출 -> Appstore Connect 업로드까지 해아 하며,
PC -> XCode Project 과정에서  ipa -> Appstore Connect 업로드에 필요한 plist(property list) 등을 함께 Xcode Project에 담아야 합니다. 이것은 Config File로 작업할 수 있습니다.

젠킨스에 IOS Build 플러그인이 있음에도 불구하고 배치파일로 뺀 이유는 세팅 간소화 및 범용성을 위해서입니다.
(플러그인에 세팅해야 할 변수가 많음, 젠킨스가 아닌 환경에서도 쓸 수 있게끔)

**여기서 IOS 빌드 테스트 항목은 PC -> XCode Project -> ipa 추출 및 Appstore Connect 업로드까지 테스트합니다.**

**Appstore Connect 링크**
https://appstoreconnect.apple.com/

### 빌드 방법
1. 젠킨스 프로젝트 생성 후 프로젝트 구성으로 갑니다.
2. Build에 Execute Shell 작업을 추가하고 하단의 내용을 적습니다.
```
# 폴더 내 모든 걸 비우기
rm -r ${WORKSPACE}/Build/
```
3. Build에 Invoke Unity3D Editor/Editor command line arguments에 하단의 내용을 적습니다.
```
-quit -batchmode -executeMethod Jenkins.Builder.Build_IOS -config_path YOUR_CONFIG_PATH.json -output_path ${WORKSPACE} -filename Build -ios_version 1
```

4. Build에 Execute Shell 작업을 추가하고 하단의 내용을 적습니다.
```
# ipa Export
sh "ios_export_ipa.sh이 들어있는 경로" "${WORKSPACE}" "AppleTeamID"
``` 
여기서 AppleTeamID는 애플 계정에 있는 애플 팀 ID를 기입합니다. 팀 ID는 숫자와 대문자 영어로 이루어진 10개의 단어입니다. (2020.08 기준)

5. Mac OS가 설치된 PC에서 XCode를 설치 후 Preference - AppleTeamID를 등록한 뒤 빌드를 합니다.
6. **Build를 한 뒤 AppleStore Connect 사이트에 세팅한 빌드번호의 빌드가 있으면 성공**
