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
