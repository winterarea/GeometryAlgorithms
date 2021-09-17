<template>
    <Row>
    <Col span="24">
    <div class="ivu-card-holder">
        <Card :bordered="true" style="text-align:center;">
            <span>本页面仅供已取得客户端授权的用户查阅</span>   
        </Card>
    </div>
    </Col>
    <Row>
    <div ref="viewer" style="width:500px;height:500px;"></div>
    </Row>
    </Row>
</template>



<script>
//https://segmentfault.com/q/1010000040638554/a-1020000040658098
import GLOBAL from '../../api/global'
let echarts = require('../../api/echarts.min');
import * as Three from 'three'
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

export default {
    name: 'tm-echarts',
    data () {
        return {
            camera:null,
            scene:null,
            renderer:null,
            mesh:null,
            object:null
        }
    },
    methods:{
        init:function(){
            let container = this.$refs.viewer;

            this.camera = new Three.PerspectiveCamera(70, container.clientWidth/container.clientHeight, 0.01, 10);
            this.camera.position.z = 1;

            this.scene = new Three.Scene();

            let geometry = new Three.BoxGeometry(0.2, 0.2, 0.2);
            let material = new Three.MeshNormalMaterial();

            this.mesh = new Three.Mesh(geometry, material);
            this.scene.add(this.mesh);

            const manager = new Three.LoadingManager( this.loadModel );

            const loader = new OBJLoader( manager );
            var _this=this;
            loader.load( 'static/asset/cube.obj', function ( obj ) {
                _this.object=obj;
            });



            this.renderer = new Three.WebGLRenderer({antialias: true,alpha:true});
            this.renderer.setSize(container.clientWidth, container.clientHeight);
            this.renderer.setClearAlpha(0.0);
            container.appendChild(this.renderer.domElement);

            const controls = new OrbitControls( this.camera, this.renderer.domElement );
            //controls.minDistance = 200;
            //controls.maxDistance = 2000;

        },
        loadModel:function() {
            let material = new Three.MeshNormalMaterial();
            this.object.traverse( function ( child ) {
                if ( child.isMesh ) {child.material = material;child.scale.set(.15,.15,.15);}
            } );
            //this.object.position.y = - 1;
            this.scene.add( this.object );
            console.log(this.scene);
        },
        animate: function() {
            requestAnimationFrame(this.animate);
            this.mesh.rotation.x += 0.01;
            this.mesh.rotation.y += 0.02;
            this.renderer.render(this.scene, this.camera);
        }
    },
    created(){
    },
    beforeDestroy () {
    },
    mounted(){ 
        this.$parent.mainLoaded(true);//通知父节点加载完成
        this.init();
        this.animate();
    }
}
</script>
<style>

</style>
