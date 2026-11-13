import { useEffect, useMemo } from 'react';
import * as THREE from 'three';
import type { BodyRegionId } from '../../data/bodyRegions';

// A single continuous surface per organ, or per member of a paired organ.
function surface(kind: 'brain' | 'lung' | 'heart' | 'stomach') {
  const geometry = new THREE.SphereGeometry(1, 48, 32);
  const positions = geometry.attributes.position;
  for (let i = 0; i < positions.count; i++) {
    let x = positions.getX(i), y = positions.getY(i), z = positions.getZ(i);
    if (kind === 'lung') {
      x = x * (.78 - .26 * y) + .14 * (1 - y * y);
      z *= .62;
    } else if (kind === 'brain') {
      const cleft = 1 - .13 * Math.exp(-x * x * 70);
      const folds = 1 + .025 * Math.sin(y * 28 + z * 10) * Math.cos(z * 24);
      x *= 1.16 * folds; y *= .85 * cleft * folds; z *= .82 * cleft * folds;
    } else if (kind === 'heart') {
      const taper = .75 + .22 * y;
      x = x * taper - .20 * y; y *= 1.16; z *= .68 * taper;
    } else {
      const taper = .72 + .25 * y;
      x = x * taper + .38 * y * y - .15 * y; y *= 1.1; z *= .65 * taper;
    }
    positions.setXYZ(i, x, y, z);
  }
  geometry.computeVertexNormals();
  return geometry;
}

export function OrganShape({ id, color, active }: { id: BodyRegionId; color: string; active: boolean }) {
  const geometry = useMemo(() => id === 'eyes' ? null : surface(id === 'lungs' ? 'lung' : id), [id]);
  useEffect(() => () => geometry?.dispose(), [geometry]);
  const material = { color, roughness: .48, metalness: .03, clearcoat: .25,
    emissive: color, emissiveIntensity: active ? .5 : .025 };
  if (id === 'eyes') return <group>{[-1, 1].map(side => <group key={side} position={[side * .016, 0, 0]}>
    <mesh scale={[.010,.009,.009]}><sphereGeometry args={[1,24,16]}/><meshPhysicalMaterial color="#eee9e3" roughness={.28}/></mesh>
    <mesh position={[0,0,.0085]}><circleGeometry args={[.0048,24]}/><meshPhysicalMaterial {...material}/></mesh>
    <mesh position={[0,0,.0087]}><circleGeometry args={[.002,20]}/><meshBasicMaterial color="#17232c"/></mesh>
  </group>)}</group>;
  if (id === 'lungs') return <group>
    {[-1,1].map(side => <mesh key={side} geometry={geometry!}
      position={[side * .032,-.008,0]} scale={[side * .029,.058,.028]} rotation={[0,0,-side*.08]}>
      <meshPhysicalMaterial {...material}/>
    </mesh>)}
    <mesh position={[0,.046,-.008]}><cylinderGeometry args={[.004,.004,.054,16]}/><meshPhysicalMaterial color="#d6c3b7" roughness={.6}/></mesh>
    {[-1,1].map(side => <mesh key={side} position={[side*.01,.014,-.006]} rotation={[0,0,-side*.65]}>
      <cylinderGeometry args={[.003,.003,.031,12]}/><meshPhysicalMaterial color="#d6c3b7" roughness={.6}/>
    </mesh>)}
  </group>;
  const scale: [number,number,number] = id === 'brain' ? [.035,.035,.033] : id === 'heart' ? [.026,.027,.026] : [.027,.034,.026];
  return <group>
    <mesh geometry={geometry!} scale={scale} rotation={[0,0,id==='heart'?-.2:id==='stomach'?.25:0]}><meshPhysicalMaterial {...material}/></mesh>
    {id === 'heart' && <group>
      <mesh position={[.001,.028,-.004]} rotation={[0,0,.2]}><torusGeometry args={[.011,.0035,12,24,Math.PI]}/><meshPhysicalMaterial color="#ba4652" roughness={.45}/></mesh>
      <mesh position={[-.012,.027,-.005]}><cylinderGeometry args={[.0035,.0035,.022,12]}/><meshPhysicalMaterial color="#7198b6" roughness={.45}/></mesh>
    </group>}
  </group>;
}
