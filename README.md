# CameraStack
![stackcamera](https://user-images.githubusercontent.com/73415970/168471903-bfaa4135-a70f-423c-847a-79151db038f0.PNG)  

Canvas - ScreenSpace Camera를 사용중이였는데 urp로 넘어오면서 카메라에 depth 프로퍼티가 사라지고 depth only가 사라졌다.  
depth는 stack 이라는 리스트로 depth only는 overlay camera로 구현이 가능하게 바뀌어서 기존형태를 유지하면서 사용 할 수 있도록 매니저 클래스를 만들었다.  

### 사용법  
카메라에 csCamerastacking 스크립트를 넣어주고 depth 값을 넣어주면된다.
is base camera 라는 bool 값은 baseCamera인 카메라에 붙어 있는 스크립트에 체크해주면된다.  
기존 basecamera가 꺼지거나 사라지면 overlay 였던 카메라를 basecamera로 바꿔서 사용해줘야하고  
basecamera가 여러대 있는 상황을 위해 만들었다.
