# ============================================
# 작성자 : Strix
# 개요 : 버전 파일(Json 형식만 지원)을 핸들링하는 Shell입니다. 
# 설치 : JJ(Json CLI Editor,https://github.com/tidwall/jj)를 설치해야 합니다.
#
# 주의사항 : Window에서 파일 수정후 맥에서 실행시 줄바꿈 코드(EOL)를 Unix 형식으로 변경바랍니다. (Window/MAC은 안됨)
# ============================================


# 데이터 세팅
FILE_NAME=$1
EDIT_NAME=$2
METHOD_NAME=$3
ZERO_PADDING_COUNT=$4

# JJ 플러그인을 통해 변수 추출
VERSION=$(cat "$FILE_NAME" | jj "$EDIT_NAME")

if [ "$METHOD_NAME" == "GET" ] || [ "$METHOD_NAME" == "get" ]
then
	echo $VERSION
	
else
	NEW_VERSION=$VERSION;

	echo "Current File : $(cat "$FILE_NAME") METHOD_NAME : $METHOD_NAME \nNEW_VERSION : $NEW_VERSION" 

	if [ "$METHOD_NAME" == "ADD" ] || [ "$METHOD_NAME" == "add" ]
	then
		# 변수에 1을 더합니다.
		NEW_VERSION=$((${VERSION} + 1))
	elif [ "$METHOD_NAME" == "SUB" ] || [ "$METHOD_NAME" == "sub" ]
	then
		# 변수에 1을 더합니다.
		NEW_VERSION=$((${VERSION} - 1))
	fi

	if [ "$ZERO_PADDING_COUNT" != "0" ]
	then
		OPERATOR="%0${ZERO_PADDING_COUNT}d"
		NEW_VERSION=$(printf \""$OPERATOR\"" $NEW_VERSION)
		
		echo "Zero Fill $ADD_PREFIX // $NEW_VERSION"
	fi

	echo "$EDIT_NAME : Current : $VERSION // New : $NEW_VERSION"

	# 1을 더한 값을 변수에 저장후 덮어쓰기
	# -v = 덮어쓰기 / -p = 보기좋은 출력
	FILE_TEXT=$(cat "$FILE_NAME" | jj -r -v "$NEW_VERSION" "$EDIT_NAME" | jj -p)
		
	# 변수를 파일에 덮어쓰며 확인합니다.
	echo "$FILE_TEXT" > "$FILE_NAME"
	echo "New File : $(cat "$FILE_NAME")"
fi