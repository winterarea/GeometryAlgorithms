package com.nmaid.asset.loader;
//int[] 转byte[]:https://www.jianshu.com/p/4ffb082e892c

public class Binary {
	public static void main(String[] args) {
		byte[] buff=new byte[]{(byte) 0xc1,0x2,0x3,0x4};
		int[] result=readBit(buff,0,3);
		for(int i=0;i<result.length;i++){
			System.out.print(result[i]+",");
		}
		System.out.println();

		int[] data=new int[]{1,0,3,1,0,6,0,0,4,0,0};
		data=new int[]{255,255,255,1};
		byte[] byteRes=writeBit(data,0,10);
		for(int i=0;i<byteRes.length;i++){
			System.out.print((0xff&byteRes[i])+",");
		}
		System.out.println();
		
		result=readBit(byteRes,0,10);
		for(int i=0;i<result.length;i++){
			System.out.print(result[i]+",");
		}
		System.out.println();
	}
	public static int[] readBit(byte[] buff,int pos,int size){
		int i,index=0,code=0;
		int bitCount=buff.length*8;
		int[] result=new int[bitCount/size];
		int lastPos=bitCount-bitCount%size;
		while(pos<lastPos){
			code=0;
			for(i=0;i<size;i++){
				if((buff[pos>>3]&1<<(pos&7))>0){
					code|=1<<i;
				}
				pos++;
			}
			result[index++]=code;
		}
		return result;
	}
	public static byte[] writeBit(int[] buff,int pos,int size){
		int i,j;
		int resLength=(buff.length*size-pos)>>3;
		byte[] result=new byte[resLength];
		int resIndex=0;
		
		int code=0;
		for(i=0;i<buff.length;i++){
			for(j=0;j<size;j++){
				if(((buff[i]>>j)&1)>0){
					code|=1<<pos;
				}
				pos++;
				if(pos>=8){
					result[resIndex++]=(byte) code;
					code=0;
					pos=pos%8;
				}
			}
		}
		return result;
	}
}
