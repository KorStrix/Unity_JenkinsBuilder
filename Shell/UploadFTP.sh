# ============================================
# 작성자 : Strix
# 개요 : LFTP를 통해 FTP에 업로드하는 스크립트입니다.
#	   LFTP는 병렬 전송 및 실패 시 어느정도 자동화를(경로가 없으면 경로생성 등) 지원하기 때문에 채택하였습니다.	    
#
# 설치 : LFTP를 설치해야 합니다.
#	- 공식 홈페이지 : https://lftp.yar.ru/	
#
# 주의사항 : Window에서 파일 수정후 맥에서 실행시 줄바꿈 코드(EOL)를 Unix 형식으로 변경바랍니다. (Window/MAC은 안됨)
# ============================================


# 데이터 세팅 - FTP
HOST=$1
USER=$2
PASS=$3

CLEAR_TARGET_DIR_IF_EXIST=$4
SOURCE_DIR=$5
TARGET_DIR=$6


# TARGETDIR이 이미 있으면 비우기 유무
if [ $CLEAR_TARGET_DIR_IF_EXIST == "true" ]
then
  echo "Clear Start // Target Dir : $TARGET_DIR"
  
  lftp -f "
  open $HOST
  user $USER $PASS
  set ftp:ssl-allow no;
  rm -r -f "$TARGET_DIR";
  bye
  "
fi


echo "Upload Start // SourceDir : $SOURCE_DIR TargetDir : $TARGET_DIR"

lftp -f "
open $HOST
user $USER $PASS
set ftp:ssl-allow no;
mirror -R --parallel=50 $SOURCE_DIR $TARGET_DIR
bye
"
