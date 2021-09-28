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
    <Col span="24">
    <div ref="viewer" style="width:500px;height:500px;"></div>
    </Col>
    </Row>
    </Row>
</template>



<script>
//https://segmentfault.com/q/1010000040638554/a-1020000040658098
//平面映射：https://codingdict.com/questions/93242
//多边形缩放：https://blog.csdn.net/asukasmallriver/article/details/81032783
//Tomacat中使用Socket：https://blog.csdn.net/weixin_34217773/article/details/92607742
import GLOBAL from '../../api/global'
let echarts = require('../../api/echarts.min');
import * as Three from 'three'
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

import { HorizontalBlurShader } from 'three/examples/jsm/shaders/HorizontalBlurShader.js';
import { VerticalBlurShader } from 'three/examples/jsm/shaders/VerticalBlurShader.js';

export default {
    name: 'tm-echarts',
    data () {
        return {
            camera:null,
            scene:null,
            renderer:null,
            mesh:null,
            object:null,
            velocity:0.01,
            state : {
                shadow: {
                    blur: 3.5,
                    darkness: 1,
                    opacity: 1,
                },
                plane: {
                    color: '#ffffff',
                    opacity: 0,
                },
                showWireframe: false,
            },
            PLANE_WIDTH :2.5,
            PLANE_HEIGHT  :2.5,
            CAMERA_HEIGHT :2.5,
            shadowGroup:null,
            renderTarget:null,
            renderTargetBlur:null,
            shadowCamera:null,
            cameraHelper:null,
            depthMaterial:null,
            horizontalBlurMaterial:null,
            verticalBlurMaterial:null,
            plane:null,
            blurPlane:null,
            fillPlane:null

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
            //https://stackoverflow.com/questions/41507098/how-can-i-set-headers-on-an-outgoing-three-textureloader-request
            loader.requestHeader = { NMA: GLOBAL.nma };
            var _this=this;

            this.$axios.get(
                GLOBAL.jwtUrl+'api/nma/asset/get?id='+_this.$route.params["id"],{headers:{"NMA":GLOBAL.nma}}
            )
            .then(res=>{
                if(res.data&&res.data.fileExt==".obj"){
                    loader.load( GLOBAL.jwtUrl+'api/nma/asset/download?id='+_this.$route.params["id"], function ( obj ) {
                        _this.object=obj;
                    });
                }
            })
            .catch(function (error) {
                _this.$parent.mainLoaded(true);
            });

            this.setShadowGroud();

            this.renderer = new Three.WebGLRenderer({antialias: true,alpha:true});
            this.renderer.setSize(container.clientWidth, container.clientHeight);
            this.renderer.setClearAlpha(0.0);
            container.appendChild(this.renderer.domElement);

            const controls = new OrbitControls( this.camera, this.renderer.domElement );
            //controls.minDistance = 200;
            //controls.maxDistance = 2000;

        },
        setShadowGroud:function(){
            // the container, if you need to move the plane just move this
            this.shadowGroup = new Three.Group();
            this.shadowGroup.position.y = - 0.3;
            this.scene.add( this.shadowGroup );

            // the render target that will show the shadows in the plane texture
            this.renderTarget = new Three.WebGLRenderTarget( 512, 512 );
            this.renderTarget.texture.generateMipmaps = false;

            // the render target that we will use to blur the first render target
            this.renderTargetBlur = new Three.WebGLRenderTarget( 512, 512 );
            this.renderTargetBlur.texture.generateMipmaps = false;


            // make a plane and make it face up
            const planeGeometry = new Three.PlaneGeometry( this.PLANE_WIDTH, this.PLANE_HEIGHT ).rotateX( Math.PI / 2 );
            const planeMaterial = new Three.MeshBasicMaterial( {
                map: this.renderTarget.texture,
                opacity: this.state.shadow.opacity,
                transparent: true,
                depthWrite: false,
            } );
            this.plane = new Three.Mesh( planeGeometry, planeMaterial );
            // make sure it's rendered after the fillPlane
            this.plane.renderOrder = 1;
            this.shadowGroup.add( this.plane );

            // the y from the texture is flipped!
            this.plane.scale.y = - 1;

            // the plane onto which to blur the texture
            this.blurPlane = new Three.Mesh( planeGeometry );
            this.blurPlane.visible = false;
            this.shadowGroup.add( this.blurPlane );

            // the plane with the color of the ground
            const fillPlaneMaterial = new Three.MeshBasicMaterial( {
                color: this.state.plane.color,
                opacity: this.state.plane.opacity,
                transparent: true,
                depthWrite: false,
            } );
            this.fillPlane = new Three.Mesh( planeGeometry, fillPlaneMaterial );
            this.fillPlane.rotateX( Math.PI );
            this.shadowGroup.add( this.fillPlane );

            // the camera to render the depth material from
            this.shadowCamera = new Three.OrthographicCamera( - this.PLANE_WIDTH / 2, this.PLANE_WIDTH / 2, this.PLANE_HEIGHT / 2, - this.PLANE_HEIGHT / 2, 0, this.CAMERA_HEIGHT );
            this.shadowCamera.rotation.x = Math.PI / 2; // get the camera to look up
            this.shadowGroup.add( this.shadowCamera );

            this.cameraHelper = new Three.CameraHelper( this.shadowCamera );

            // like MeshDepthMaterial, but goes from black to transparent
            var _this=this;
            this.depthMaterial = new Three.MeshDepthMaterial();
            this.depthMaterial.userData.darkness = { value: this.state.shadow.darkness };
            this.depthMaterial.onBeforeCompile = function ( shader ) {

                shader.uniforms.darkness = _this.depthMaterial.userData.darkness;
                shader.fragmentShader = /* glsl */`
                    uniform float darkness;
                    ${shader.fragmentShader.replace(
                'gl_FragColor = vec4( vec3( 1.0 - fragCoordZ ), opacity );',
                'gl_FragColor = vec4( vec3( 0.0 ), ( 1.0 - fragCoordZ ) * darkness );'
            )}
                `;

            };

            this.depthMaterial.depthTest = false;
            this.depthMaterial.depthWrite = false;

            this.horizontalBlurMaterial = new Three.ShaderMaterial( HorizontalBlurShader );
            this.horizontalBlurMaterial.depthTest = false;

            this.verticalBlurMaterial = new Three.ShaderMaterial( VerticalBlurShader );
            this.verticalBlurMaterial.depthTest = false;
        },
        blurShadow:function ( amount ) {

            this.blurPlane.visible = true;

            // blur horizontally and draw in the renderTargetBlur
            this.blurPlane.material = this.horizontalBlurMaterial;
            this.blurPlane.material.uniforms.tDiffuse.value = this.renderTarget.texture;
            this.horizontalBlurMaterial.uniforms.h.value = amount * 1 / 256;

            this.renderer.setRenderTarget( this.renderTargetBlur );
            this.renderer.render( this.blurPlane, this.shadowCamera );

            // blur vertically and draw in the main renderTarget
            this.blurPlane.material = this.verticalBlurMaterial;
            this.blurPlane.material.uniforms.tDiffuse.value = this.renderTargetBlur.texture;
            this.verticalBlurMaterial.uniforms.v.value = amount * 1 / 256;

            this.renderer.setRenderTarget( this.renderTarget );
            this.renderer.render( this.blurPlane, this.shadowCamera );

            this.blurPlane.visible = false;

        },
        loadModel:function() {
            let material = new Three.MeshNormalMaterial();
            this.object.traverse( function ( child ) {
                if ( child.isMesh ) {child.material = material;child.scale.set(.15,.15,.15);}
            } );
            this.object.position.y = -0.15;
            this.scene.add( this.object );
        },
        renderShadow:function(){
            // remove the background
            const initialBackground = this.scene.background;
            this.scene.background = null;

            // force the depthMaterial to everything
            this.cameraHelper.visible = false;
            this.scene.overrideMaterial = this.depthMaterial;

            // render to the render target to get the depths
            this.renderer.setRenderTarget( this.renderTarget );
            this.renderer.render( this.scene, this.shadowCamera );

            // and reset the override material
            this.scene.overrideMaterial = null;
            this.cameraHelper.visible = true;

            this.blurShadow( this.state.shadow.blur );

            // a second pass to reduce the artifacts
            // (0.4 is the minimum blur amout so that the artifacts are gone)
            this.blurShadow( this.state.shadow.blur * 0.4 );

            // reset and render the normal scene
            this.renderer.setRenderTarget( null );
            this.scene.background = initialBackground;
        },
        animate: function() {
            requestAnimationFrame(this.animate);
            this.mesh.rotation.x += 0.01;
            this.mesh.rotation.y += 0.02;
            if(this.object){
                this.object.position.y+=this.velocity;
                this.velocity-=0.0001;
                if(this.object.position.y<-0.15)
                    this.velocity=0.01;
            }
            this.renderShadow();
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
    },
    watch: {
        '$route': function (to, from) {
            if(to.name=="assetDetail"){
                this.showFilter=false;
                let id=to.params["id"];
            }
        }
    }
}
</script>
<style>

</style>
