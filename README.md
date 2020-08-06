# Unity_JenkinsBuilder
젠킨스에서 실행하는 유니티 빌드 스크립트 에셋입니다.

정확하게는 외부에서 커맨드라인을 통해 원하는 빌드를 뽑는 툴이며 편의상 Jenkins Builder로 칭합니다.


## 1. 설치방법

### 주의) 유니티 에디터로 작업하시지 마시고 파일 탐색기를 통해 작업하시기 바랍니다.

### 1-1. UnityProject/Packages/manifest.json 파일 수정

manifest.json파일을 찾으신 뒤
원하시는 라인에 하단의 라인을 추가합니다. 
```
,"com.korstrix.jenkinsbuilder":"https://github.com/KorStrix/Unity_JenkinsBuilder.git"
```
","를 포함하는 이유는 json 형식이라 형식을 맞춰야 하기 때문입니다.
Unity에서 packages를 인식 후 알아서 형식이 변경됩니다.


### 1-2. github 프로젝트를 다운받은 후 Unity Project에 통째로 탑재

1. github에서 이 프로젝트를 다운받습니다.
2. 설치할 유니티 프로젝트 폴더에 Pacakages 폴더 안에 이 프로젝트 폴더를 넣습니다.
3. 유니티 에디터를 새로고침 합니다.

### 해당 프로젝트에 정상 설치되었는지 확인 방법
Tools/Build에 메뉴가 뜨면 성공!

---
## 2. 빌드 테스트

### 2-1. Build Config 생성 및 Editor에서 바로 빌드 테스트
1. 유니티 에디터의 상단 탭 - Tools/Build/Create Build Config Exmaple File을 생성합니다.
2. Build Config는 Json 형식으로 이루어져 있으며, 각 항목당 설명은 스크립트에 주석[(링크)](https://github.com/KorStrix/Unity_JenkinsBuilder/blob/master/Editor/BuildConfig.cs)으로 달았습니다.
이를 참고하며 Build Config 파일을 프로젝트에 맞게 수정합니다.
3. 수정 완료 후 Tools/Build/Show Jenkins Builder Window를 통해 윈도우를 엽니다.
4. 아까 작성한 Config File Path를 설정 후 빌드 파일을 어디에 생성할지 세팅합니다.
5. Android or IOS Build를 합니다.
* 빌드 하기 전 주의사항 ) 프로젝트의 Platform이 해당 플랫폼인지 확인합니다.
* AOS는 APK를 바로 빌드할 수 있지만, IOS의 경우 유니티에선 Xcode Project로 Export밖에 못합니다.

6. APK or XCode Project가 세팅한 Output path에 나오면 성공!

### 2-2. 젠킨스 - 안드로이드 연결
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


3. Build를 한 뒤 Archive에 APK파일이 있으면 성공!
