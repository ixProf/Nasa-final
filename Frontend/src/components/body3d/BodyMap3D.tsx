import { useUiText } from "../../i18n/useUiText";
/**
 * BodyMap3D.tsx
 *
 * Interactive 3D anatomical body map built with React Three Fiber + Drei.
 *
 * Architecture
 * ───────────────────────────────────────────────────────────────────────────
 *  • Canvas (R3F)
 *      └── Lighting rig & Environment
 *      └── ModelErrorBoundary
 *            └── Suspense (with professional loading indicator)
 *                  └── ModelLoader
 *                        ├── AnatomicalModel (when external GLB is verified & present)
 *                        └── BiometricHologramModel (procedural 3D anatomical model when no GLB or on fallback)
 *      └── OrbitControls (rotate, zoom, pan)
 *
 * Robust GLB Asset Resolution
 * ───────────────────────────────────────────────────────────────────────────
 *  1. Checks candidate paths: `/models/human-anatomy.glb`, `/models/anatomy.glb`, etc.
 *  2. Inspects Content-Type & binary magic bytes ('glTF') to distinguish real GLB
 *     files from SPA HTML fallback responses (which return status 200 with index.html).
 *  3. Configures Draco decompression decoder for compressed GLBs.
 *  4. Gracefully degrades to BiometricHologramModel if no external GLB exists,
 *     ensuring all 5 anatomical regions remain 100% interactive and synced with the sidebar.
 */

import React, {
  Suspense,
  useRef,
  useState,
  useCallback,
  useEffect,
  useMemo,
  useContext,
} from 'react';
import { Canvas, useFrame, useThree } from '@react-three/fiber';
import {
  OrbitControls,
  useGLTF,
  Environment,
  Html,
  Float,
  Sparkles,
} from '@react-three/drei';
import { OrganShape } from './OrganShape';
import { AnatomySymptomsContext } from './AnatomySymptomsContext';
import { REGION_SYMPTOMS, organSymptomSeverity, ORGAN_SEVERITY_COLORS } from '../../services/anatomy';
import * as THREE from 'three';
import type { GLTF } from 'three-stdlib';

import {
  BODY_REGIONS,
  findRegionByMeshName,
  type BodyRegion,
  type BodyRegionId,
} from '../../data/bodyRegions';
import { useLanguage } from '../../context/LanguageContext';
import { useTelemetry } from '../../context/TelemetryContext';

// ─── Draco Decoder Configuration ──────────────────────────────────────────────
// Set default Google CDN decoder path for Draco compressed GLTF models
try {
  useGLTF.setDecoderPath('https://www.gstatic.com/draco/versioned/decoders/1.5.5/');
} catch {
  // Ignore in environments where setDecoderPath is not available
}

// ─── Candidate Model URLs ─────────────────────────────────────────────────────
const CANDIDATE_MODEL_PATHS = [
  '/models/ecorche.glb',
  '/models/human-anatomy.glb',
  '/models/anatomy.glb',
  '/models/body.glb',
  '/models/human.glb',
];

/**
 * Validates whether a URL serves an actual GLB binary or glTF JSON file.
 * Returns true ONLY if:
 *  - HTTP status is 200 / 206
 *  - Content-Type is NOT text/html (which happens when Vite/nginx serves index.html on 404 fallback)
 *  - First 4 bytes match the GLB magic header 'glTF' (0x67, 0x6c, 0x54, 0x46) or '{' (JSON glTF)
 */
async function verifyGlbAsset(url: string): Promise<boolean> {
  try {
    const res = await fetch(url, {
      method: 'GET',
      headers: { Range: 'bytes=0-3' },
      cache: 'no-cache',
    });

    if (!res.ok) return false;

    // Check Content-Type — if Vite/nginx returns index.html for missing files, reject it!
    const contentType = res.headers.get('content-type') || '';
    if (contentType.toLowerCase().includes('text/html')) {
      return false;
    }

    const reader = res.body?.getReader();
    if (!reader) return false;
    const prefix: number[] = [];
    while (prefix.length < 4) {
      const { value, done } = await reader.read();
      if (done) break;
      if (value) prefix.push(...value.subarray(0, 4 - prefix.length));
    }
    void reader.cancel();
    if (prefix.length < 4) return false;
    const bytes = new Uint8Array(prefix);
    // GLB magic number: 0x46546C67 ('glTF' in little-endian ASCII)
    const isGlb =
      bytes[0] === 0x67 &&
      bytes[1] === 0x6c &&
      bytes[2] === 0x54 &&
      bytes[3] === 0x46;

    // glTF JSON starts with '{' (0x7B)
    const isGltfJson = bytes[0] === 0x7b;

    return isGlb || isGltfJson;
  } catch {
    return false;
  }
}

// ─── Props ───────────────────────────────────────────────────────────────────

export interface BodyMap3DProps {
  /** Currently selected anatomical region */
  selectedRegionId: BodyRegionId | null;
  /** Called when user clicks a mesh region */
  onRegionSelect: (id: BodyRegionId) => void;
  /** Height of the canvas container */
  height?: string | number;
  /** Show ambient cosmic sparkles */
  showParticles?: boolean;
}

// ─── Floating Tooltip ─────────────────────────────────────────────────────────

const RegionTooltip: React.FC<{ region: BodyRegion; isAr: boolean }> = ({
  region,
  isAr,
}) => (
  <div
    style={{
      background: 'rgba(2, 6, 20, 0.90)',
      border: `1px solid ${region.hoverColor}60`,
      borderRadius: '8px',
      padding: '4px 10px',
      pointerEvents: 'none',
      whiteSpace: 'nowrap',
      backdropFilter: 'blur(10px)',
      color: '#ffffff',
      fontSize: '11px',
      fontWeight: 700,
      letterSpacing: '0.02em',
      boxShadow: `0 4px 18px ${region.hoverColor}30`,
    }}
  >
    {isAr ? region.nameAr : region.nameEn}
  </div>
);

// ─── Interactive GLB Mesh ─────────────────────────────────────────────────────

interface AnatomicalMeshProps {
  mesh: THREE.Mesh;
  region: BodyRegion;
  isSelected: boolean;
  isHovered: boolean;
  onPointerEnter: () => void;
  onPointerLeave: () => void;
  onClick: () => void;
  isAr: boolean;
}

const AnatomicalMesh: React.FC<AnatomicalMeshProps> = React.memo(
  ({
    mesh,
    region,
    isSelected,
    isHovered,
    onPointerEnter,
    onPointerLeave,
    onClick,
    isAr,
  }) => {
    const meshRef = useRef<THREE.Mesh>(null!);

    const { geometry, material } = useMemo(() => {
      const geo = mesh.geometry.clone();
      const mat = new THREE.MeshStandardMaterial({
        color: new THREE.Color(region.baseColor),
        roughness: 0.45,
        metalness: 0.2,
        transparent: true,
        opacity: 0.9,
      });
      return { geometry: geo, material: mat };
    }, [mesh, region]);

    useFrame((_, delta) => {
      if (!meshRef.current) return;
      const mat = meshRef.current.material as THREE.MeshStandardMaterial;

      const targetColor = isSelected
        ? region.selectedColor
        : isHovered
        ? region.hoverColor
        : region.baseColor;

      const targetEmissive = isSelected
        ? region.selectedColor
        : isHovered
        ? region.hoverColor
        : '#000000';

      const targetEmissiveIntensity = isSelected ? 0.5 : isHovered ? 0.3 : 0;
      const targetOpacity = isSelected ? 1 : isHovered ? 0.95 : 0.85;

      mat.color.lerp(new THREE.Color(targetColor), delta * 6);
      mat.emissive.lerp(new THREE.Color(targetEmissive), delta * 6);
      mat.emissiveIntensity = THREE.MathUtils.lerp(
        mat.emissiveIntensity,
        targetEmissiveIntensity,
        delta * 6
      );
      mat.opacity = THREE.MathUtils.lerp(mat.opacity, targetOpacity, delta * 6);

      const targetScale = isSelected ? 1.03 : isHovered ? 1.015 : 1.0;
      meshRef.current.scale.lerp(
        new THREE.Vector3(targetScale, targetScale, targetScale),
        delta * 8
      );
    });

    useEffect(() => {
      return () => {
        geometry.dispose();
        material.dispose();
      };
    }, [geometry, material]);

    return (
      <mesh
        ref={meshRef}
        geometry={geometry}
        material={material}
        position={mesh.position}
        rotation={mesh.rotation}
        scale={mesh.scale}
        onPointerEnter={(e) => {
          e.stopPropagation();
          onPointerEnter();
        }}
        onPointerLeave={(e) => {
          e.stopPropagation();
          onPointerLeave();
        }}
        onClick={(e) => {
          e.stopPropagation();
          onClick();
        }}
        castShadow
        receiveShadow
      >
        {(isHovered || isSelected) && (
          <Html position={[0, 0, 0]} center distanceFactor={4} occlude={false}>
            <RegionTooltip region={region} isAr={isAr} />
          </Html>
        )}
      </mesh>
    );
  }
);
AnatomicalMesh.displayName = 'AnatomicalMesh';

// ─── Default Non-Mapped Mesh ──────────────────────────────────────────────────

interface DefaultMeshProps {
  mesh: THREE.Mesh;
}

const DefaultMesh: React.FC<DefaultMeshProps> = React.memo(({ mesh }) => {
  const { geometry, material } = useMemo(() => {
    const geo = mesh.geometry.clone();
    const mat = new THREE.MeshStandardMaterial({
      color: new THREE.Color('#1e3a5f'),
      roughness: 0.7,
      metalness: 0.1,
      transparent: true,
      opacity: 0.45,
    });
    return { geometry: geo, material: mat };
  }, [mesh]);

  useEffect(() => {
    return () => {
      geometry.dispose();
      material.dispose();
    };
  }, [geometry, material]);

  return (
    <mesh
      geometry={geometry}
      material={material}
      position={mesh.position}
      rotation={mesh.rotation}
      scale={mesh.scale}
      receiveShadow
    />
  );
});
DefaultMesh.displayName = 'DefaultMesh';

// ─── External GLB Anatomical Model ────────────────────────────────────────────

interface AnatomicalModelProps {
  modelUrl: string;
  selectedRegionId: BodyRegionId | null;
  onRegionSelect: (id: BodyRegionId) => void;
  isAr: boolean;
}

const AnatomicalModel: React.FC<AnatomicalModelProps> = ({
  modelUrl,
  selectedRegionId,
  onRegionSelect,
  isAr,
}) => {
  // Pass true to enable Draco decoding
  const gltf = useGLTF(modelUrl, true) as GLTF & {
    nodes: Record<string, THREE.Object3D>;
  };

  const [hoveredRegionId, setHoveredRegionId] = useState<BodyRegionId | null>(null);

  const { mappedMeshes, unmappedMeshes } = useMemo(() => {
    const mapped: Array<{ mesh: THREE.Mesh; region: BodyRegion }> = [];
    const unmapped: THREE.Mesh[] = [];

    gltf.scene.traverse((child) => {
      if (!(child instanceof THREE.Mesh)) return;
      const region = findRegionByMeshName(child.name);
      if (region) {
        mapped.push({ mesh: child, region });
      } else {
        unmapped.push(child);
      }
    });

    return { mappedMeshes: mapped, unmappedMeshes: unmapped };
  }, [gltf]);

  const { camera } = useThree();
  useEffect(() => {
    const box = new THREE.Box3().setFromObject(gltf.scene);
    const size = new THREE.Vector3();
    const center = new THREE.Vector3();
    box.getSize(size);
    box.getCenter(center);

    // Center model at origin
    gltf.scene.position.x = -center.x;
    gltf.scene.position.y = -center.y;
    gltf.scene.position.z = -center.z;

    const maxDim = Math.max(size.x, size.y, size.z);
    const fov = (camera as THREE.PerspectiveCamera).fov * (Math.PI / 180);
    let cameraZ = Math.abs(maxDim / 2 / Math.tan(fov / 2));
    cameraZ *= 1.4;

    camera.position.set(0, 0, cameraZ);
    camera.lookAt(0, 0, 0);
    camera.near = cameraZ / 100;
    camera.far = cameraZ * 10;
    camera.updateProjectionMatrix();
  }, [gltf, camera]);

  return (
    <group>
      {mappedMeshes.map(({ mesh, region }, i) => (
        <AnatomicalMesh
          key={`${region.id}-${i}`}
          mesh={mesh}
          region={region}
          isSelected={selectedRegionId === region.id}
          isHovered={hoveredRegionId === region.id}
          onPointerEnter={() => setHoveredRegionId(region.id)}
          onPointerLeave={() => setHoveredRegionId(null)}
          onClick={() => onRegionSelect(region.id)}
          isAr={isAr}
        />
      ))}

      {unmappedMeshes.map((mesh, i) => (
        <DefaultMesh key={`unmapped-${i}`} mesh={mesh} />
      ))}
    </group>
  );
};

// ─── Biometric 3D Procedural Hologram (Fallback & Standalone Mode) ───────────

// Keep the original hierarchy and materials of the detailed model intact.
const EcorcheModel: React.FC<AnatomicalModelProps> = ({ modelUrl, selectedRegionId, onRegionSelect }) => {
  const gltf = useGLTF(modelUrl);
  const { camera, size: viewport, controls, invalidate } = useThree();
  const symptoms = useContext(AnatomySymptomsContext);
  const { scene, center, size } = useMemo(() => {
    const scene = gltf.scene.clone(true);
    scene.updateMatrixWorld(true);
    const bounds = new THREE.Box3().setFromObject(scene);
    return { scene, center: bounds.getCenter(new THREE.Vector3()), size: bounds.getSize(new THREE.Vector3()) };
  }, [gltf.scene]);
  useEffect(() => {
    const perspective = camera as THREE.PerspectiveCamera;
    const tangent = Math.tan(perspective.fov * Math.PI / 360);
    const aspect = viewport.width / Math.max(1, viewport.height);
    // Reserve space for the title and credits while fitting both axes.
    const distance = Math.max(size.y / (.78 * 2 * tangent),
      Math.max(size.x, size.y * .62) / (.85 * 2 * tangent * aspect)) + size.z;
    camera.position.set(0, 0.35, distance);
    camera.lookAt(0, 0.35, 0);
    camera.near = distance / 100;
    camera.far = distance * 10;
    camera.updateProjectionMatrix();
    const orbit = controls as unknown as { target: THREE.Vector3; update: () => void } | null;
    orbit?.target.set(0, .35, 0);
    orbit?.update();
    invalidate();
  }, [camera, size, viewport.width, viewport.height, controls, invalidate]);

  // The source contains no organs. These are deliberately schematic overlays,
  // placed in front of the body rather than implying precise internal anatomy.
  const organs: Array<{ id: BodyRegionId; x: number; y: number }> = [
    { id: 'brain', x: 0, y: .95 }, { id: 'eyes', x: 0, y: .90 },
    { id: 'lungs', x: 0, y: .72 }, { id: 'heart', x: .04, y: .68 },
    { id: 'stomach', x: .04, y: .58 },
  ];
  return <group>
    <primitive object={scene} position={[-center.x, -center.y + 0.35, -center.z]} />
    {organs.map(({ id, x, y }) => {
      const selected = selectedRegionId === id;
      const linked = symptoms.filter(symptom=>REGION_SYMPTOMS[id].includes(symptom.code));
      const severity = organSymptomSeverity(id, symptoms);
      const color = severity ? ORGAN_SEVERITY_COLORS[severity] : selected ? '#67e8f9' : undefined;
      const organColor: Record<BodyRegionId, string> = {brain:'#c8a4b6', eyes:'#6ca1a3', lungs:'#c58e96', heart:'#bf5262', stomach:'#cba885'};
      const s = size.y;
      return <group key={id} position={[x * size.x, (y - 0.5) * size.y + 0.35, size.z * 0.65]}>
        <group scale={s} onClick={event=>{event.stopPropagation();onRegionSelect(id);}}>
          <OrganShape id={id} color={color || organColor[id]} active={selected || linked.length>0}/>
          {linked.length>0 && <mesh scale={id==='lungs'?[.082,.085,.04]:id==='eyes'?[.04,.025,.02]:[.05,.05,.035]} raycast={()=>{}}><sphereGeometry args={[1,24,16]}/><meshBasicMaterial color={color} transparent opacity={.12} depthWrite={false}/></mesh>}
        </group>

      </group>;
    })}
  </group>;
};

interface BiometricHologramProps {
  selectedRegionId: BodyRegionId | null;
  onRegionSelect: (id: BodyRegionId) => void;
  isAr: boolean;
}

/**
 * High-fidelity, interactive 3D biometric hologram of the human anatomy.
 * Features an anatomical translucent glass silhouette and 5 independently
 * clickable, hoverable anatomical organs.
 */
const BiometricHologramModel: React.FC<BiometricHologramProps> = ({
  selectedRegionId,
  onRegionSelect,
  isAr,
}) => {
  const [hoveredId, setHoveredId] = useState<BodyRegionId | null>(null);
  const { vitals } = useTelemetry();
  const heartMeshRef = useRef<THREE.Group>(null);

  // Dynamic heartbeat animation matching telemetry HR
  const bpm = parseFloat(vitals?.HR?.value || '72') || 72;
  const pulseSpeed = (bpm / 60) * Math.PI * 2;

  useFrame(({ clock }) => {
    if (heartMeshRef.current) {
      const t = clock.getElapsedTime() * pulseSpeed;
      const beat = 1 + Math.sin(t) * 0.06;
      heartMeshRef.current.scale.set(beat, beat, beat);
    }
  });

  // Reusable body shell material
  const bodyMaterial = useMemo(
    () =>
      new THREE.MeshStandardMaterial({
        color: new THREE.Color('#0b254a'),
        roughness: 0.75,
        metalness: 0.25,
        transparent: true,
        opacity: 0.38,
        wireframe: false,
      }),
    []
  );

  const wireframeMaterial = useMemo(
    () =>
      new THREE.MeshBasicMaterial({
        color: new THREE.Color('#38bdf8'),
        wireframe: true,
        transparent: true,
        opacity: 0.15,
      }),
    []
  );

  useEffect(() => {
    return () => {
      bodyMaterial.dispose();
      wireframeMaterial.dispose();
    };
  }, [bodyMaterial, wireframeMaterial]);

  // Procedural geometry for body sections
  const torsoGeo = useMemo(() => new THREE.CylinderGeometry(0.38, 0.28, 1.1, 24), []);
  const pelvisGeo = useMemo(() => new THREE.CylinderGeometry(0.28, 0.32, 0.45, 24), []);
  const headGeo = useMemo(() => new THREE.SphereGeometry(0.28, 24, 24), []);
  const neckGeo = useMemo(() => new THREE.CylinderGeometry(0.12, 0.14, 0.22, 16), []);
  const armGeo = useMemo(() => new THREE.CylinderGeometry(0.08, 0.07, 1.1, 16), []);
  const legGeo = useMemo(() => new THREE.CylinderGeometry(0.12, 0.09, 1.4, 16), []);

  useEffect(() => {
    return () => {
      torsoGeo.dispose();
      pelvisGeo.dispose();
      headGeo.dispose();
      neckGeo.dispose();
      armGeo.dispose();
      legGeo.dispose();
    };
  }, [torsoGeo, pelvisGeo, headGeo, neckGeo, armGeo, legGeo]);

  return (
    <group position={[0, -0.2, 0]}>
      {/* ── Translucent Human Anatomical Shell ──────────────────────────────── */}
      {/* Head / Cranium */}
      <mesh geometry={headGeo} material={bodyMaterial} position={[0, 1.62, 0]} />
      <mesh geometry={headGeo} material={wireframeMaterial} position={[0, 1.62, 0]} />

      {/* Neck */}
      <mesh geometry={neckGeo} material={bodyMaterial} position={[0, 1.34, 0]} />

      {/* Thorax / Ribcage */}
      <mesh geometry={torsoGeo} material={bodyMaterial} position={[0, 0.72, 0]} />
      <mesh geometry={torsoGeo} material={wireframeMaterial} position={[0, 0.72, 0]} />

      {/* Pelvis / Abdomen */}
      <mesh geometry={pelvisGeo} material={bodyMaterial} position={[0, -0.05, 0]} />

      {/* Arms */}
      <mesh
        geometry={armGeo}
        material={bodyMaterial}
        position={[-0.48, 0.65, 0]}
        rotation={[0, 0, 0.12]}
      />
      <mesh
        geometry={armGeo}
        material={bodyMaterial}
        position={[0.48, 0.65, 0]}
        rotation={[0, 0, -0.12]}
      />

      {/* Legs */}
      <mesh
        geometry={legGeo}
        material={bodyMaterial}
        position={[-0.2, -0.95, 0]}
        rotation={[0, 0, -0.03]}
      />
      <mesh
        geometry={legGeo}
        material={bodyMaterial}
        position={[0.2, -0.95, 0]}
        rotation={[0, 0, 0.03]}
      />

      {/* ── Organ 1: Brain (Cerebrum & Cerebellum) ─────────────────────────── */}
      {(() => {
        const region = BODY_REGIONS.find((r) => r.id === 'brain')!;
        const isSelected = selectedRegionId === 'brain';
        const isHovered = hoveredId === 'brain';
        const color = isSelected
          ? region.selectedColor
          : isHovered
          ? region.hoverColor
          : region.selectedColor;

        return (
          <Float speed={2} rotationIntensity={0.08} floatIntensity={0.06}>
            <group
              position={[0, 1.62, 0]}
              onPointerEnter={(e) => {
                e.stopPropagation();
                setHoveredId('brain');
              }}
              onPointerLeave={(e) => {
                e.stopPropagation();
                setHoveredId(null);
              }}
              onClick={(e) => {
                e.stopPropagation();
                onRegionSelect('brain');
              }}
            >
              {/* Left Hemisphere */}
              <mesh position={[-0.08, 0.02, 0]}>
                <sphereGeometry args={[0.13, 20, 20]} />
                <meshStandardMaterial
                  color={color}
                  emissive={color}
                  emissiveIntensity={isSelected ? 0.7 : isHovered ? 0.45 : 0.25}
                  roughness={0.35}
                  metalness={0.2}
                />
              </mesh>
              {/* Right Hemisphere */}
              <mesh position={[0.08, 0.02, 0]}>
                <sphereGeometry args={[0.13, 20, 20]} />
                <meshStandardMaterial
                  color={color}
                  emissive={color}
                  emissiveIntensity={isSelected ? 0.7 : isHovered ? 0.45 : 0.25}
                  roughness={0.35}
                  metalness={0.2}
                />
              </mesh>
              {/* Cerebellum */}
              <mesh position={[0, -0.07, -0.06]}>
                <sphereGeometry args={[0.08, 16, 16]} />
                <meshStandardMaterial
                  color={color}
                  emissive={color}
                  emissiveIntensity={isSelected ? 0.7 : isHovered ? 0.45 : 0.2}
                />
              </mesh>
              {(isHovered || isSelected) && (
                <Html center distanceFactor={4} occlude={false}>
                  <RegionTooltip region={region} isAr={isAr} />
                </Html>
              )}
            </group>
          </Float>
        );
      })()}

      {/* ── Organ 2: Eyes / Optic System ───────────────────────────────────── */}
      {(() => {
        const region = BODY_REGIONS.find((r) => r.id === 'eyes')!;
        const isSelected = selectedRegionId === 'eyes';
        const isHovered = hoveredId === 'eyes';
        const color = isSelected
          ? region.selectedColor
          : isHovered
          ? region.hoverColor
          : region.selectedColor;

        return (
          <group
            position={[0, 1.62, 0.22]}
            onPointerEnter={(e) => {
              e.stopPropagation();
              setHoveredId('eyes');
            }}
            onPointerLeave={(e) => {
              e.stopPropagation();
              setHoveredId(null);
            }}
            onClick={(e) => {
              e.stopPropagation();
              onRegionSelect('eyes');
            }}
          >
            {/* Left Eye */}
            <mesh position={[-0.1, 0, 0]}>
              <sphereGeometry args={[0.045, 16, 16]} />
              <meshStandardMaterial
                color={color}
                emissive={color}
                emissiveIntensity={isSelected ? 0.8 : isHovered ? 0.5 : 0.3}
              />
            </mesh>
            {/* Right Eye */}
            <mesh position={[0.1, 0, 0]}>
              <sphereGeometry args={[0.045, 16, 16]} />
              <meshStandardMaterial
                color={color}
                emissive={color}
                emissiveIntensity={isSelected ? 0.8 : isHovered ? 0.5 : 0.3}
              />
            </mesh>
            {(isHovered || isSelected) && (
              <Html center distanceFactor={4} occlude={false}>
                <RegionTooltip region={region} isAr={isAr} />
              </Html>
            )}
          </group>
        );
      })()}

      {/* ── Organ 3: Lungs (Pulmonary Lobes) ───────────────────────────────── */}
      {(() => {
        const region = BODY_REGIONS.find((r) => r.id === 'lungs')!;
        const isSelected = selectedRegionId === 'lungs';
        const isHovered = hoveredId === 'lungs';
        const color = isSelected
          ? region.selectedColor
          : isHovered
          ? region.hoverColor
          : region.selectedColor;

        return (
          <group
            position={[0, 0.78, 0]}
            onPointerEnter={(e) => {
              e.stopPropagation();
              setHoveredId('lungs');
            }}
            onPointerLeave={(e) => {
              e.stopPropagation();
              setHoveredId(null);
            }}
            onClick={(e) => {
              e.stopPropagation();
              onRegionSelect('lungs');
            }}
          >
            {/* Left Lung */}
            <mesh position={[-0.16, 0, 0]} rotation={[0, 0, -0.08]}>
              <capsuleGeometry args={[0.09, 0.28, 16, 16]} />
              <meshStandardMaterial
                color={color}
                emissive={color}
                emissiveIntensity={isSelected ? 0.65 : isHovered ? 0.4 : 0.22}
                transparent
                opacity={0.88}
              />
            </mesh>
            {/* Right Lung */}
            <mesh position={[0.16, 0, 0]} rotation={[0, 0, 0.08]}>
              <capsuleGeometry args={[0.09, 0.28, 16, 16]} />
              <meshStandardMaterial
                color={color}
                emissive={color}
                emissiveIntensity={isSelected ? 0.65 : isHovered ? 0.4 : 0.22}
                transparent
                opacity={0.88}
              />
            </mesh>
            {(isHovered || isSelected) && (
              <Html center distanceFactor={4} occlude={false}>
                <RegionTooltip region={region} isAr={isAr} />
              </Html>
            )}
          </group>
        );
      })()}

      {/* ── Organ 4: Heart (Cardiac Muscle & Aorta) ────────────────────────── */}
      {(() => {
        const region = BODY_REGIONS.find((r) => r.id === 'heart')!;
        const isSelected = selectedRegionId === 'heart';
        const isHovered = hoveredId === 'heart';
        const color = isSelected
          ? region.selectedColor
          : isHovered
          ? region.hoverColor
          : region.selectedColor;

        return (
          <group
            ref={heartMeshRef}
            position={[0.06, 0.74, 0.1]}
            onPointerEnter={(e) => {
              e.stopPropagation();
              setHoveredId('heart');
            }}
            onPointerLeave={(e) => {
              e.stopPropagation();
              setHoveredId(null);
            }}
            onClick={(e) => {
              e.stopPropagation();
              onRegionSelect('heart');
            }}
          >
            {/* Cardiac Core */}
            <mesh rotation={[0, 0, -0.2]}>
              <sphereGeometry args={[0.085, 20, 20]} />
              <meshStandardMaterial
                color={color}
                emissive={color}
                emissiveIntensity={isSelected ? 0.8 : isHovered ? 0.5 : 0.3}
                roughness={0.3}
              />
            </mesh>
            {/* Aorta Arch */}
            <mesh position={[0.01, 0.08, 0]} rotation={[0, 0, 0.4]}>
              <torusGeometry args={[0.04, 0.018, 12, 18, Math.PI]} />
              <meshStandardMaterial color={color} emissive={color} emissiveIntensity={0.4} />
            </mesh>
            {(isHovered || isSelected) && (
              <Html center distanceFactor={4} occlude={false}>
                <RegionTooltip region={region} isAr={isAr} />
              </Html>
            )}
          </group>
        );
      })()}

      {/* ── Organ 5: GI System (Stomach & Pancreas) ─────────────────────────── */}
      {(() => {
        const region = BODY_REGIONS.find((r) => r.id === 'stomach')!;
        const isSelected = selectedRegionId === 'stomach';
        const isHovered = hoveredId === 'stomach';
        const color = isSelected
          ? region.selectedColor
          : isHovered
          ? region.hoverColor
          : region.selectedColor;

        return (
          <group
            position={[-0.04, 0.34, 0.08]}
            onPointerEnter={(e) => {
              e.stopPropagation();
              setHoveredId('stomach');
            }}
            onPointerLeave={(e) => {
              e.stopPropagation();
              setHoveredId(null);
            }}
            onClick={(e) => {
              e.stopPropagation();
              onRegionSelect('stomach');
            }}
          >
            {/* Stomach curvature */}
            <mesh rotation={[0, 0, 0.35]}>
              <capsuleGeometry args={[0.08, 0.18, 16, 16]} />
              <meshStandardMaterial
                color={color}
                emissive={color}
                emissiveIntensity={isSelected ? 0.7 : isHovered ? 0.45 : 0.22}
                roughness={0.4}
              />
            </mesh>
            {/* Duodenum / Pancreas branch */}
            <mesh position={[0.1, -0.06, 0]} rotation={[0, 0, -0.5]}>
              <cylinderGeometry args={[0.035, 0.035, 0.14, 12]} />
              <meshStandardMaterial color={color} emissive={color} emissiveIntensity={0.25} />
            </mesh>
            {(isHovered || isSelected) && (
              <Html center distanceFactor={4} occlude={false}>
                <RegionTooltip region={region} isAr={isAr} />
              </Html>
            )}
          </group>
        );
      })()}
    </group>
  );
};

// ─── Professional Loading Indicator ───────────────────────────────────────────

const ModelLoadingIndicator: React.FC<{ isAr: boolean }> = ({ isAr }) => (
  <Html center>
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: '12px',
        color: '#94a3b8',
        background: 'rgba(2, 6, 20, 0.85)',
        padding: '16px 24px',
        borderRadius: '16px',
        border: '1px solid rgba(56, 189, 248, 0.25)',
        backdropFilter: 'blur(12px)',
      }}
    >
      <div
        style={{
          width: 38,
          height: 38,
          borderRadius: '50%',
          border: '3px solid rgba(56, 189, 248, 0.2)',
          borderTopColor: '#38bdf8',
          animation: 'spin 0.9s linear infinite',
        }}
      />
      <span style={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.08em', color: '#e2e8f0' }}>
        {isAr ? 'جارٍ تحميل نموذج التشريح ثلاثي الأبعاد…' : 'INITIALISING 3D ANATOMY…'}
      </span>
      <style>{`@keyframes spin{to{transform:rotate(360deg)}}`}</style>
    </div>
  </Html>
);

// ─── Error Boundary ───────────────────────────────────────────────────────────

interface ModelErrorState {
  hasError: boolean;
  errorMessage?: string;
}

class ModelErrorBoundary extends React.Component<
  React.PropsWithChildren<{
    selectedRegionId: BodyRegionId | null;
    onRegionSelect: (id: BodyRegionId) => void;
    isAr: boolean;
  }>,
  ModelErrorState
> {
  constructor(props: any) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ModelErrorState {
    return { hasError: true, errorMessage: error.message };
  }

  componentDidCatch(error: Error) {
    console.warn('[BodyMap3D] Model render error intercepted:', error);
  }

  render() {
    if (this.state.hasError) {
      // Instead of an empty canvas, smoothly render the procedural anatomical model!
      return (
        <BiometricHologramModel
          selectedRegionId={this.props.selectedRegionId}
          onRegionSelect={this.props.onRegionSelect}
          isAr={this.props.isAr}
        />
      );
    }
    return this.props.children;
  }
}

// ─── ModelLoader Component ───────────────────────────────────────────────────

interface ModelLoaderProps {
  modelUrl: string | null;
  selectedRegionId: BodyRegionId | null;
  onRegionSelect: (id: BodyRegionId) => void;
  isAr: boolean;
}

const ModelLoader: React.FC<ModelLoaderProps> = ({
  modelUrl,
  selectedRegionId,
  onRegionSelect,
  isAr,
}) => {
  if (modelUrl?.endsWith('/ecorche.glb')) {
    return <EcorcheModel modelUrl={modelUrl} selectedRegionId={selectedRegionId} onRegionSelect={onRegionSelect} isAr={isAr} />;
  }
  if (modelUrl) {
    return (
      <AnatomicalModel
        modelUrl={modelUrl}
        selectedRegionId={selectedRegionId}
        onRegionSelect={onRegionSelect}
        isAr={isAr}
      />
    );
  }

  return (
    <BiometricHologramModel
      selectedRegionId={selectedRegionId}
      onRegionSelect={onRegionSelect}
      isAr={isAr}
    />
  );
};

// ─── Lighting Rig ─────────────────────────────────────────────────────────────

const LightingRig: React.FC = () => (
  <>
    {/* Key light — cool cyan-blue front */}
    <directionalLight position={[2, 4, 3]} intensity={1.5} color="#e0f2fe" castShadow />
    {/* Rim light — accent back */}
    <directionalLight position={[-3, 2, -3]} intensity={0.7} color="#fbcfe8" />
    {/* Fill light — soft upward fill */}
    <directionalLight position={[0, -3, 2]} intensity={0.4} color="#38bdf8" />
    {/* Ambient illumination */}
    <ambientLight intensity={0.45} color="#0c2340" />
    {/* Center highlight point */}
    <pointLight position={[0, 0.5, 2.2]} intensity={0.6} color="#38bdf8" distance={5} />
  </>
);

// ─── Main Export: BodyMap3D ───────────────────────────────────────────────────

export const BodyMap3D: React.FC<BodyMap3DProps> = ({
  selectedRegionId,
  onRegionSelect,
  height = '100%',
  showParticles = true,
}) => {
  const tx = useUiText();
  const { language } = useLanguage();
  const isAr = language === 'ar';

  const [verifiedModelUrl, setVerifiedModelUrl] = useState<string | null>(null);
  const [isVerifying, setIsVerifying] = useState(true);

  // Probe candidates for an authentic GLB file
  useEffect(() => {
    let cancelled = false;

    async function probeCandidateGlbs() {
      for (const path of CANDIDATE_MODEL_PATHS) {
        const isAuthentic = await verifyGlbAsset(path);
        if (isAuthentic && !cancelled) {
          console.log(`[BodyMap3D] Found authentic GLB asset at: ${path}`);
          setVerifiedModelUrl(path);
          setIsVerifying(false);
          return;
        }
      }

      if (!cancelled) {
        setVerifiedModelUrl(null);
        setIsVerifying(false);
      }
    }

    probeCandidateGlbs();

    return () => {
      cancelled = true;
    };
  }, []);

  const handleRegionSelect = useCallback(
    (id: BodyRegionId) => {
      onRegionSelect(id);
    },
    [onRegionSelect]
  );

  return (
    <div style={{ width: '100%', height, position: 'relative' }}>
      {/* Top Floating Status Pill */}
      <div
        style={{
          position: 'absolute',
          top: 10,
          left: '50%',
          transform: 'translateX(-50%)',
          zIndex: 10,
          background: 'rgba(2, 6, 20, 0.82)',
          border: verifiedModelUrl
            ? '1px solid rgba(52, 211, 153, 0.45)'
            : '1px solid rgba(56, 189, 248, 0.35)',
          borderRadius: '9999px',
          padding: '4px 14px',
          fontSize: '11px',
          fontWeight: 700,
          color: verifiedModelUrl ? '#6ee7b7' : '#bae6fd',
          pointerEvents: 'none',
          backdropFilter: 'blur(10px)',
          display: 'flex',
          alignItems: 'center',
          gap: '6px',
          boxShadow: '0 4px 16px rgba(0, 0, 0, 0.5)',
        }}
      >
        <span
          style={{
            width: 7,
            height: 7,
            borderRadius: '50%',
            background: verifiedModelUrl ? '#34d399' : '#38bdf8',
            boxShadow: `0 0 8px ${verifiedModelUrl ? '#34d399' : '#38bdf8'}`,
          }}
        />
        <span>
          {verifiedModelUrl ? (isAr ? 'تشريح تفاعلي' : 'Interactive anatomy') : (isAr ? 'خريطة الجسم' : 'Body map')}
        </span>
      </div>

      <Canvas
        dpr={[1, 1.5]}
        frameloop={verifiedModelUrl?.endsWith('/ecorche.glb') ? 'demand' : 'always'}
        shadows
        camera={{ position: [0, 0.4, 3.8], fov: 42, near: 0.1, far: 50 }}
        gl={{ antialias: true, alpha: true, powerPreference: 'high-performance' }}
        style={{ background: 'transparent' }}
      >
        <LightingRig />
        {!verifiedModelUrl?.endsWith('/ecorche.glb') && <Environment preset="night" background={false} />}

        {showParticles && !verifiedModelUrl?.endsWith('/ecorche.glb') && (
          <Sparkles
            count={45}
            size={0.7}
            speed={0.25}
            color="#38bdf8"
            opacity={0.35}
            scale={[3.5, 4.5, 3.5]}
          />
        )}

        <ModelErrorBoundary
          selectedRegionId={selectedRegionId}
          onRegionSelect={handleRegionSelect}
          isAr={isAr}
        >
          <Suspense fallback={<ModelLoadingIndicator isAr={isAr} />}>
            {!isVerifying && (
              <ModelLoader
                modelUrl={verifiedModelUrl}
                selectedRegionId={selectedRegionId}
                onRegionSelect={handleRegionSelect}
                isAr={isAr}
              />
            )}
          </Suspense>
        </ModelErrorBoundary>

        <OrbitControls
          enablePan={false}
          enableZoom={true}
          enableRotate={true}
          minDistance={0.5}
          maxDistance={100}
          minPolarAngle={Math.PI / 4}
          maxPolarAngle={(Math.PI * 3) / 4}
          target={[0, 0.35, 0]}
          makeDefault
        />
      </Canvas>
      {verifiedModelUrl?.endsWith('/ecorche.glb') && <div className="absolute bottom-2 start-2 end-2 z-10 rounded-lg bg-slate-950/90 px-2 py-1 text-[10px] text-slate-300">
        <span>{isAr ? 'الأعضاء المعروضة توضيحية ومواضعها تقريبية. ' : 'Organ overlays are schematic and approximately positioned. '}</span>
        <a href="https://github.com/Charleeh/ecorche/blob/main/ATTRIBUTION.md" target="_blank" rel="noreferrer" className="underline text-cyan-300">Écorché · CC BY-SA 4.0</a>
      </div>}
    </div>
  );
};
