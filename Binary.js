//https://juejin.cn/post/6844903956238303240
/**
*@global {Uint8Array} arr - 即字节流如[0xff,0xaa,0x10...]
*@global {Number} pos - 二进制流索引，比如0的时候为这个buffer的二进制第0位
*
*@param {Number} size - 位数，范围可为1~31
*@return {Number}
*/
function read(size) {
    var i, code = 0;
    for (i = 0; i < size; i++) {
        if (arr[pos >> 3] & 1 << (pos & 7)) {
            code |= 1 << i;
        }
        pos++;
    }
    return code;
}
readBit:function(buff,pos,size){
    var i,code=0;
    var bitCount=buff.length*8;
    var result=[];
    while(pos<bitCount){
        code=0;
        for(i=0;i<size;i++){
            if(buff[pos>>3]&1<<(pos&7)){
                code|=1<<i;
            }
            pos++;
        }
        result.push(code);
    }
    return result;
},
writeBit:function(buff,pos,size){
    var i,j;
    var resLength=(buff.length*size-pos)>>3;
    var result=[];
    //console.log(resLength)
    var code=0,pos=0;
    for(i=0;i<buff.length;i++){
        for(j=0;j<size;j++){
            if((buff[i]>>j)&1){
                code|=1<<pos;

            }
            pos++;
            //console.log(pos)
            if(pos>=8){
                result.push(code);
                code=0;
                pos=pos%8;
            }
        }
    }
    return result;
},
