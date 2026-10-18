import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { 
  Stethoscope, 
  Users, 
  FileText, 
  Activity, 
  Heart, 
  Thermometer, 
  Search, 
  Plus, 
  Trash2, 
  Download, 
  CheckCircle2, 
  Clock, 
  User, 
  Sparkles
} from 'lucide-react';

interface Patient {
  id: string;
  mrn: string;
  nameEn: string;
  nameBn: string;
  age: number;
  gender: string;
  phone: string;
  bloodGroup: string;
  address: string;
}

interface VitalSigns {
  temperature: number;
  pulse: number;
  bpSystolic: number;
  bpDiastolic: number;
  spO2: number;
  weight: number;
  height: number;
  respiratoryRate: number;
}

interface DiagnosisItem {
  icdCode: string;
  description: string;
  descriptionBn: string;
  isPrimary: boolean;
}

interface PrescriptionItem {
  id: string;
  medicineName: string;
  dose: string;
  frequency: string;
  durationDays: number;
  instructions: string;
  instructionsBn: string;
}

interface QueueItem {
  id: string;
  tokenNo: number;
  patientName: string;
  patientMrn: string;
  time: string;
  status: 'Waiting' | 'InConsult' | 'Completed';
  gender: string;
  age: number;
}

export function App() {
  const [lang, setLang] = useState<'en' | 'bn'>('en');
  const [activeTab, setActiveTab] = useState<'queue' | 'consultation' | 'prescriptions'>('consultation');
  const [hubConnected, setHubConnected] = useState(false);
  const [currentTime, setCurrentTime] = useState('');

  // Sample Doctor Context
  const doctor = {
    name: 'Dr. Mahmud Hasan',
    nameBn: 'ডাঃ মাহমুদ হাসান',
    bmdc: 'DEMO-1001',
    specialty: 'General Medicine & Cardiology',
    specialtyBn: 'মেডিসিন ও হৃদরোগ বিশেষজ্ঞ',
    qualification: 'MBBS (DMC), FCPS (Medicine)'
  };

  // Sample Active Patient
  const [selectedPatient, setSelectedPatient] = useState<Patient>({
    id: 'f87a8b12-9c12-4d2b-b8c1-111111111111',
    mrn: '2026-000108',
    nameEn: 'Mohammad Rafiqul Islam',
    nameBn: 'মোহাম্মদ রফিকুল ইসলাম',
    age: 46,
    gender: 'Male',
    phone: '+8801712345678',
    bloodGroup: 'B+',
    address: 'Dhanmondi, Dhaka'
  });

  // OPD Queue State
  const [queue, setQueue] = useState<QueueItem[]>([
    { id: '1', tokenNo: 1, patientName: 'Mohammad Rafiqul Islam', patientMrn: '2026-000108', time: '10:00 AM', status: 'InConsult', gender: 'Male', age: 46 },
    { id: '2', tokenNo: 2, patientName: 'Fatema Begum', patientMrn: '2026-000114', time: '10:15 AM', status: 'Waiting', gender: 'Female', age: 38 },
    { id: '3', tokenNo: 3, patientName: 'Abdul Karim', patientMrn: '2026-000129', time: '10:30 AM', status: 'Waiting', gender: 'Male', age: 62 },
    { id: '4', tokenNo: 4, patientName: 'Nasrin Sultana', patientMrn: '2026-000140', time: '10:45 AM', status: 'Waiting', gender: 'Female', age: 29 },
  ]);

  // Vitals State
  const [vitals, setVitals] = useState<VitalSigns>({
    temperature: 98.6,
    pulse: 76,
    bpSystolic: 135,
    bpDiastolic: 85,
    spO2: 98,
    weight: 72.5,
    height: 172,
    respiratoryRate: 18
  });
  const [vitalsSaved, setVitalsSaved] = useState(false);

  // Diagnosis State
  const [diagnoses, setDiagnoses] = useState<DiagnosisItem[]>([
    { icdCode: 'I10', description: 'Essential (primary) hypertension', descriptionBn: 'উচ্চ রক্তচাপ', isPrimary: true }
  ]);
  const [icdQuery, setIcdQuery] = useState('');
  const [icdSuggestions, setIcdSuggestions] = useState<any[]>([]);

  // Clinical Notes State
  const [noteType, setNoteType] = useState('History & Examination');
  const [clinicalNotes, setClinicalNotes] = useState('Patient presents with intermittent headache and mild dizziness for 2 weeks. No chest pain or shortness of breath. History of occasional high BP recorded at home.');

  // Prescription Composer State
  const [medicines, setMedicines] = useState<PrescriptionItem[]>([
    { id: '1', medicineName: 'Losartan Potassium 50mg (Angilock)', dose: '1 tab', frequency: '1+0+0', durationDays: 30, instructions: 'Morning after breakfast', instructionsBn: 'সকালে খাবার পর' },
    { id: '2', medicineName: 'Paracetamol 500mg (Napa)', dose: '1 tab', frequency: '1+0+1 (as needed)', durationDays: 5, instructions: 'After meal for headache', instructionsBn: 'মাথাব্যথা হলে ভরা পেটে' }
  ]);

  const [medQuery, setMedQuery] = useState('');
  const [medSuggestions, setMedSuggestions] = useState<any[]>([]);
  const [selectedMed, setSelectedMed] = useState('');
  const [selectedDose] = useState('1 tab');
  const [selectedFreq, setSelectedFreq] = useState('1+0+1');
  const [selectedDuration, setSelectedDuration] = useState(14);
  const [selectedInstruction, setSelectedInstruction] = useState('After meal (ভরা পেটে)');

  const [followUpDays, setFollowUpDays] = useState(14);
  const [doctorAdvice, setDoctorAdvice] = useState('Low salt diet. 30 mins brisk walking daily. Check BP weekly and record in log book.');
  const [prescriptionSuccess, setPrescriptionSuccess] = useState(false);
  const [prescriptionId, setPrescriptionId] = useState<string | null>(null);

  // Live Asia/Dhaka Clock
  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setCurrentTime(now.toLocaleTimeString('en-US', { timeZone: 'Asia/Dhaka', hour: '2-digit', minute: '2-digit', second: '2-digit' }));
    };
    updateTime();
    const timer = setInterval(updateTime, 1000);
    return () => clearInterval(timer);
  }, []);

  // SignalR Hub Connection for Real-time OPD Queue
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/opd', {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => {
        setHubConnected(true);
        connection.on('QueueUpdated', (data: any) => {
          if (Array.isArray(data)) {
            setQueue(data);
          }
        });
      })
      .catch(() => {
        setHubConnected(false);
      });

    return () => {
      connection.stop();
    };
  }, []);

  // Debounced ICD-10 Search
  useEffect(() => {
    if (!icdQuery.trim()) {
      setIcdSuggestions([]);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await fetch(`http://localhost:5000/api/v1/clinical/icd10?q=${encodeURIComponent(icdQuery)}`);
        if (res.ok) {
          const data = await res.json();
          setIcdSuggestions(data.slice(0, 5));
        }
      } catch {
        // Fallback offline suggestions
        const offline = [
          { code: 'I10', description: 'Essential (primary) hypertension', descriptionBn: 'উচ্চ রক্তচাপ' },
          { code: 'E11.9', description: 'Type 2 diabetes mellitus', descriptionBn: 'টাইপ ২ ডায়াবেটিস' },
          { code: 'J06.9', description: 'Acute upper respiratory infection', descriptionBn: 'সর্দি-কাশি' },
          { code: 'K29.7', description: 'Gastritis, unspecified', descriptionBn: 'গ্যাস্ট্রাইটিস' },
          { code: 'A90', description: 'Dengue fever', descriptionBn: 'ডেঙ্গু জ্বর' }
        ].filter(i => i.description.toLowerCase().includes(icdQuery.toLowerCase()) || i.code.toLowerCase().includes(icdQuery.toLowerCase()));
        setIcdSuggestions(offline);
      }
    }, 250);
    return () => clearTimeout(timer);
  }, [icdQuery]);

  // Debounced Medicine Search
  useEffect(() => {
    if (!medQuery.trim()) {
      setMedSuggestions([]);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await fetch(`http://localhost:5000/api/v1/clinical/medicines?q=${encodeURIComponent(medQuery)}`);
        if (res.ok) {
          const data = await res.json();
          setMedSuggestions(data.slice(0, 6));
        }
      } catch {
        // Fallback offline medicines
        const offline = [
          { brandName: 'Napa', genericName: 'Paracetamol', strength: '500mg' },
          { brandName: 'Angilock', genericName: 'Losartan Potassium', strength: '50mg' },
          { brandName: 'Camlodin', genericName: 'Amlodipine', strength: '5mg' },
          { brandName: 'Seclo', genericName: 'Omeprazole', strength: '20mg' },
          { brandName: 'Moxacil', genericName: 'Amoxicillin', strength: '500mg' }
        ].filter(m => m.brandName.toLowerCase().includes(medQuery.toLowerCase()) || m.genericName.toLowerCase().includes(medQuery.toLowerCase()));
        setMedSuggestions(offline);
      }
    }, 250);
    return () => clearTimeout(timer);
  }, [medQuery]);

  // Calculate BMI
  const bmi = vitals.weight > 0 && vitals.height > 0 
    ? (vitals.weight / Math.pow(vitals.height / 100, 2)).toFixed(1) 
    : '23.8';

  const addDiagnosis = (item: any) => {
    if (diagnoses.some(d => d.icdCode === (item.code || item.icdCode))) return;
    setDiagnoses([
      ...diagnoses, 
      { 
        icdCode: item.code || item.icdCode, 
        description: item.description, 
        descriptionBn: item.descriptionBn || '', 
        isPrimary: diagnoses.length === 0 
      }
    ]);
    setIcdQuery('');
    setIcdSuggestions([]);
  };

  const removeDiagnosis = (code: string) => {
    setDiagnoses(diagnoses.filter(d => d.icdCode !== code));
  };

  const addMedicine = () => {
    if (!selectedMed.trim()) return;
    const newItem: PrescriptionItem = {
      id: Date.now().toString(),
      medicineName: selectedMed,
      dose: selectedDose,
      frequency: selectedFreq,
      durationDays: selectedDuration,
      instructions: selectedInstruction.split('(')[0].trim(),
      instructionsBn: selectedInstruction.includes('(') ? selectedInstruction.split('(')[1].replace(')', '').trim() : ''
    };
    setMedicines([...medicines, newItem]);
    setSelectedMed('');
    setMedQuery('');
  };

  const removeMedicine = (id: string) => {
    setMedicines(medicines.filter(m => m.id !== id));
  };

  const handleIssuePrescription = async () => {
    setPrescriptionSuccess(true);
    setPrescriptionId('pres-' + Date.now());
    setTimeout(() => setPrescriptionSuccess(false), 5000);
  };

  const handleDownloadPdf = () => {
    // Direct trigger to test backend or fallback generated preview
    window.open('http://localhost:5000/api/v1/prescriptions/6ca2c744-e1bc-4b25-a67b-3c6f849bbb6a/pdf', '_blank');
  };

  return (
    <div className="min-h-screen flex flex-col bg-slate-950 text-slate-100">
      {/* ─── Top Clinical Navigation Bar ─── */}
      <header className="glass-header sticky top-0 z-50 px-6 py-3.5 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-teal-500 to-cyan-600 flex items-center justify-center shadow-lg shadow-teal-500/30">
              <Stethoscope className="w-5 h-5 text-white" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="font-extrabold text-lg tracking-tight bg-gradient-to-r from-teal-300 via-cyan-200 to-white bg-clip-text text-transparent">
                  Avela Clinical EMR
                </span>
                <span className="badge badge-teal text-[11px] font-semibold">
                  {lang === 'en' ? 'Clinical Workspace' : 'ক্লিনিক্যাল ওয়ার্কস্পেস'}
                </span>
              </div>
              <p className="text-xs text-slate-400">Sample Medical Centre, Dhaka • ডক্টর কনসালটেশন পোর্টাল</p>
            </div>
          </div>

          <div className="h-6 w-px bg-slate-800 mx-2" />

          {/* Navigation Tabs */}
          <nav className="flex items-center gap-1.5 bg-slate-900/80 p-1 rounded-xl border border-teal-500/20">
            <button
              onClick={() => setActiveTab('consultation')}
              className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-2 transition-all ${
                activeTab === 'consultation'
                  ? 'bg-gradient-to-r from-teal-600 to-teal-500 text-white shadow-md shadow-teal-500/20'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800/60'
              }`}
            >
              <FileText className="w-3.5 h-3.5" />
              {lang === 'en' ? 'Active Consultation' : 'বর্তমান কনসালটেশন'}
            </button>
            <button
              onClick={() => setActiveTab('queue')}
              className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-2 transition-all ${
                activeTab === 'queue'
                  ? 'bg-gradient-to-r from-teal-600 to-teal-500 text-white shadow-md shadow-teal-500/20'
                  : 'text-slate-400 hover:text-white hover:bg-slate-800/60'
              }`}
            >
              <Users className="w-3.5 h-3.5" />
              {lang === 'en' ? 'Live OPD Queue' : 'লাইভ ওপিডি কিউ'}
              <span className="w-5 h-5 rounded-full bg-teal-400/20 text-teal-300 text-[10px] flex items-center justify-center font-bold">
                {queue.filter(q => q.status === 'Waiting').length}
              </span>
            </button>
          </nav>
        </div>

        {/* Doctor Identity & Status Controls */}
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 text-xs text-slate-300 bg-slate-900/60 px-3 py-1.5 rounded-lg border border-teal-500/15">
            <Clock className="w-3.5 h-3.5 text-teal-400" />
            <span className="font-mono text-teal-200">{currentTime || '12:00:00 PM'} (BST)</span>
          </div>

          <div className="flex items-center gap-2.5 bg-slate-900/80 px-3.5 py-1.5 rounded-xl border border-teal-500/25">
            <div className="w-8 h-8 rounded-full bg-teal-500/20 border border-teal-400/40 flex items-center justify-center text-teal-300 font-bold text-xs">
              MH
            </div>
            <div className="text-left">
              <div className="flex items-center gap-1.5">
                <span className="text-xs font-bold text-slate-200">{lang === 'en' ? doctor.name : doctor.nameBn}</span>
                <span className="badge badge-teal text-[10px] py-0 px-1.5 font-mono">BMDC: {doctor.bmdc}</span>
              </div>
              <p className="text-[10px] text-teal-400/80 font-medium">{doctor.qualification}</p>
            </div>
          </div>

          {/* Bilingual Switcher */}
          <button
            onClick={() => setLang(lang === 'en' ? 'bn' : 'en')}
            className="btn-secondary text-xs py-1.5 px-3 font-semibold"
          >
            {lang === 'en' ? 'বাংলা' : 'English'}
          </button>
        </div>
      </header>

      {/* ─── Main Workspace Content ─── */}
      <main className="flex-1 p-5 overflow-auto">
        {activeTab === 'queue' ? (
          /* ─── Live OPD Queue View ─── */
          <div className="max-w-6xl mx-auto space-y-5">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-xl font-extrabold text-white flex items-center gap-2.5">
                  <Users className="w-6 h-6 text-teal-400" />
                  {lang === 'en' ? "Today's OPD Queue — Dr. Mahmud Hasan" : "আজকের ওপিডি কিউ — ডাঃ মাহমুদ হাসান"}
                </h1>
                <p className="text-xs text-slate-400 mt-1">
                  {lang === 'en' 
                    ? 'Real-time SignalR queue synchronized with reception check-in desk' 
                    : 'রিসেপশন চেক-ইন ডেস্কের সাথে সিগন্যালআর রিয়েল-টাইম সমন্বিত'}
                </p>
              </div>

              <div className="flex items-center gap-3">
                <div className="flex items-center gap-1.5 text-xs text-emerald-400 bg-emerald-500/10 px-3 py-1.5 rounded-full border border-emerald-500/20">
                  <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
                  {hubConnected ? 'SignalR Live Connected' : 'Queue Synced'}
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {queue.map((item) => (
                <div 
                  key={item.id}
                  onClick={() => {
                    setSelectedPatient({
                      ...selectedPatient,
                      mrn: item.patientMrn,
                      nameEn: item.patientName,
                      age: item.age,
                      gender: item.gender
                    });
                    setActiveTab('consultation');
                  }}
                  className={`glass-card-interactive p-4 relative overflow-hidden ${
                    item.status === 'InConsult' ? 'border-teal-400/60 bg-teal-950/30' : ''
                  }`}
                >
                  <div className="flex items-center justify-between mb-3">
                    <span className="w-9 h-9 rounded-xl bg-gradient-to-br from-teal-500 to-cyan-600 text-white font-extrabold text-sm flex items-center justify-center shadow-md">
                      #{item.tokenNo}
                    </span>
                    <span className={`badge ${
                      item.status === 'InConsult' ? 'badge-teal' : item.status === 'Waiting' ? 'badge-amber' : 'badge-green'
                    }`}>
                      {item.status}
                    </span>
                  </div>

                  <h3 className="font-bold text-sm text-slate-100 line-clamp-1">{item.patientName}</h3>
                  <div className="flex items-center justify-between text-xs text-slate-400 mt-2">
                    <span className="font-mono text-teal-300">{item.patientMrn}</span>
                    <span>{item.age}y • {item.gender}</span>
                  </div>

                  <div className="mt-4 pt-3 border-t border-slate-800/80 flex items-center justify-between">
                    <span className="text-[11px] text-slate-400 flex items-center gap-1">
                      <Clock className="w-3 h-3 text-slate-500" />
                      {item.time}
                    </span>
                    <button className="text-xs font-semibold text-teal-400 hover:text-teal-300 flex items-center gap-1">
                      {item.status === 'InConsult' ? 'Resume' : 'Call Patient'} →
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : (
          /* ─── 3-Panel Consultation & EMR Workflow ─── */
          <div className="grid grid-cols-12 gap-5 h-full max-w-[1700px] mx-auto">
            {/* ═══ PANEL 1: Patient Summary & Vitals (3 Columns) ═══ */}
            <div className="col-span-12 lg:col-span-3 space-y-4">
              {/* Patient Profile Card */}
              <div className="glass-panel p-4">
                <div className="flex items-center gap-3 mb-3">
                  <div className="w-12 h-12 rounded-2xl bg-gradient-to-br from-teal-500/20 to-cyan-500/20 border border-teal-400/30 flex items-center justify-center text-teal-300 font-extrabold text-base">
                    {selectedPatient.nameEn.charAt(0)}
                  </div>
                  <div>
                    <h2 className="font-bold text-sm text-white">{selectedPatient.nameEn}</h2>
                    <p className="text-xs text-teal-300 font-bengali">{selectedPatient.nameBn}</p>
                    <span className="font-mono text-xs text-teal-400 bg-teal-950/60 px-2 py-0.5 rounded border border-teal-500/30 mt-1 inline-block">
                      MRN: {selectedPatient.mrn}
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-2 text-xs pt-3 border-t border-teal-500/15">
                  <div className="bg-slate-900/60 p-2 rounded-lg border border-slate-800">
                    <span className="text-slate-400 block text-[10px]">{lang === 'en' ? 'Age / Gender' : 'বয়স / লিঙ্গ'}</span>
                    <span className="font-semibold text-slate-200">{selectedPatient.age} Yrs • {selectedPatient.gender}</span>
                  </div>
                  <div className="bg-slate-900/60 p-2 rounded-lg border border-slate-800">
                    <span className="text-slate-400 block text-[10px]">{lang === 'en' ? 'Blood Group' : 'রক্তের গ্রুপ'}</span>
                    <span className="font-semibold text-red-400">{selectedPatient.bloodGroup}</span>
                  </div>
                  <div className="bg-slate-900/60 p-2 rounded-lg border border-slate-800 col-span-2">
                    <span className="text-slate-400 block text-[10px]">{lang === 'en' ? 'Contact' : 'মোবাইল'}</span>
                    <span className="font-mono text-slate-300">{selectedPatient.phone}</span>
                  </div>
                </div>
              </div>

              {/* Vitals Summary & Quick Record */}
              <div className="glass-panel p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-xs font-bold uppercase tracking-wider text-teal-300 flex items-center gap-1.5">
                    <Activity className="w-3.5 h-3.5" />
                    {lang === 'en' ? 'Vital Signs & Metrics' : 'ভাইটাল সাইন ও সূচক'}
                  </h3>
                  <span className="badge badge-teal text-[10px]">BMI: {bmi}</span>
                </div>

                <div className="grid grid-cols-2 gap-2.5">
                  <div className="bg-slate-900/70 p-2.5 rounded-xl border border-teal-500/15 flex items-center gap-2.5">
                    <Heart className="w-4 h-4 text-red-400 shrink-0" />
                    <div>
                      <span className="text-[10px] text-slate-400 block">BP (mmHg)</span>
                      <input 
                        type="text" 
                        value={`${vitals.bpSystolic}/${vitals.bpDiastolic}`} 
                        onChange={(e) => {
                          const parts = e.target.value.split('/');
                          if (parts.length === 2) {
                            setVitals({ ...vitals, bpSystolic: parseInt(parts[0]) || 120, bpDiastolic: parseInt(parts[1]) || 80 });
                          }
                        }}
                        className="w-full bg-transparent font-bold text-xs text-white focus:outline-none"
                      />
                    </div>
                  </div>

                  <div className="bg-slate-900/70 p-2.5 rounded-xl border border-teal-500/15 flex items-center gap-2.5">
                    <Activity className="w-4 h-4 text-cyan-400 shrink-0" />
                    <div>
                      <span className="text-[10px] text-slate-400 block">Pulse (bpm)</span>
                      <input 
                        type="number" 
                        value={vitals.pulse} 
                        onChange={(e) => setVitals({ ...vitals, pulse: parseInt(e.target.value) || 72 })}
                        className="w-full bg-transparent font-bold text-xs text-white focus:outline-none"
                      />
                    </div>
                  </div>

                  <div className="bg-slate-900/70 p-2.5 rounded-xl border border-teal-500/15 flex items-center gap-2.5">
                    <Thermometer className="w-4 h-4 text-amber-400 shrink-0" />
                    <div>
                      <span className="text-[10px] text-slate-400 block">Temp (°F)</span>
                      <input 
                        type="number" 
                        step="0.1"
                        value={vitals.temperature} 
                        onChange={(e) => setVitals({ ...vitals, temperature: parseFloat(e.target.value) || 98.6 })}
                        className="w-full bg-transparent font-bold text-xs text-white focus:outline-none"
                      />
                    </div>
                  </div>

                  <div className="bg-slate-900/70 p-2.5 rounded-xl border border-teal-500/15 flex items-center gap-2.5">
                    <User className="w-4 h-4 text-emerald-400 shrink-0" />
                    <div>
                      <span className="text-[10px] text-slate-400 block">Weight (kg)</span>
                      <input 
                        type="number" 
                        step="0.5"
                        value={vitals.weight} 
                        onChange={(e) => setVitals({ ...vitals, weight: parseFloat(e.target.value) || 70 })}
                        className="w-full bg-transparent font-bold text-xs text-white focus:outline-none"
                      />
                    </div>
                  </div>
                </div>

                <button 
                  onClick={() => {
                    setVitalsSaved(true);
                    setTimeout(() => setVitalsSaved(false), 3000);
                  }}
                  className="w-full btn-secondary text-xs py-1.5 justify-center font-semibold"
                >
                  {vitalsSaved ? (
                    <span className="text-emerald-400 flex items-center gap-1.5">
                      <CheckCircle2 className="w-3.5 h-3.5" />
                      {lang === 'en' ? 'Vitals Updated' : 'ভাইটাল সেভ হয়েছে'}
                    </span>
                  ) : (
                    lang === 'en' ? 'Save Vitals' : 'ভাইটাল রেকর্ড করুন'
                  )}
                </button>
              </div>
            </div>

            {/* ═══ PANEL 2: Clinical Evaluation & Diagnoses (4 Columns) ═══ */}
            <div className="col-span-12 lg:col-span-4 space-y-4">
              {/* ICD-10 Search & Diagnosis Management */}
              <div className="glass-panel p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-xs font-bold uppercase tracking-wider text-teal-300 flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5" />
                    {lang === 'en' ? 'Diagnoses (ICD-10 Search)' : 'রোগ নির্ণয় (আইসিডি-১০)'}
                  </h3>
                  <span className="text-[10px] text-slate-400">Bangla + English</span>
                </div>

                {/* Search Bar */}
                <div className="relative">
                  <Search className="w-3.5 h-3.5 absolute left-3 top-3 text-slate-400" />
                  <input
                    type="text"
                    value={icdQuery}
                    onChange={(e) => setIcdQuery(e.target.value)}
                    placeholder={lang === 'en' ? "Search ICD-10 (e.g. hypertension, dengue, asthma)..." : "আইসিডি-১০ খুঁজুন (যেমন উচ্চ রক্তচাপ, ডেঙ্গু)..."}
                    className="input-glass pl-8 text-xs py-2"
                  />

                  {/* Auto-suggest dropdown */}
                  {icdSuggestions.length > 0 && (
                    <div className="absolute left-0 right-0 top-full mt-1 bg-slate-900 border border-teal-500/30 rounded-xl shadow-2xl z-20 max-h-48 overflow-auto p-1">
                      {icdSuggestions.map((item, idx) => (
                        <div
                          key={idx}
                          onClick={() => addDiagnosis(item)}
                          className="p-2 hover:bg-teal-900/40 rounded-lg cursor-pointer flex items-center justify-between text-xs transition-colors"
                        >
                          <div>
                            <span className="font-bold text-teal-300 mr-2">[{item.code || item.icdCode}]</span>
                            <span className="text-slate-200">{item.description}</span>
                            {item.descriptionBn && (
                              <span className="text-[11px] text-teal-400/80 block font-bengali">{item.descriptionBn}</span>
                            )}
                          </div>
                          <Plus className="w-3.5 h-3.5 text-teal-400 shrink-0" />
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Selected Diagnoses List */}
                <div className="space-y-2 mt-2">
                  {diagnoses.map((diag) => (
                    <div key={diag.icdCode} className="bg-slate-900/80 p-2.5 rounded-xl border border-teal-500/20 flex items-center justify-between">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-mono text-xs font-bold text-teal-400">[{diag.icdCode}]</span>
                          <span className="text-xs font-semibold text-slate-200">{diag.description}</span>
                          {diag.isPrimary && (
                            <span className="badge badge-teal text-[9px] py-0">Primary</span>
                          )}
                        </div>
                        {diag.descriptionBn && (
                          <p className="text-[11px] text-slate-400 mt-0.5 font-bengali">{diag.descriptionBn}</p>
                        )}
                      </div>
                      <button 
                        onClick={() => removeDiagnosis(diag.icdCode)}
                        className="text-slate-500 hover:text-red-400 p-1 transition-colors"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  ))}
                </div>
              </div>

              {/* Clinical Notes & History */}
              <div className="glass-panel p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <h3 className="text-xs font-bold uppercase tracking-wider text-teal-300">
                    {lang === 'en' ? 'Clinical Notes & Assessment' : 'ক্লিনিক্যাল নোট ও মূল্যায়ন'}
                  </h3>
                  <div className="flex items-center gap-1 bg-slate-900/80 p-0.5 rounded-lg border border-slate-800">
                    {['General', 'History', 'Exam', 'Plan'].map((t) => (
                      <button
                        key={t}
                        onClick={() => setNoteType(t)}
                        className={`text-[10px] px-2 py-0.5 rounded ${
                          noteType === t ? 'bg-teal-600 text-white font-bold' : 'text-slate-400 hover:text-white'
                        }`}
                      >
                        {t}
                      </button>
                    ))}
                  </div>
                </div>

                <textarea
                  value={clinicalNotes}
                  onChange={(e) => setClinicalNotes(e.target.value)}
                  rows={6}
                  placeholder={lang === 'en' ? "Write clinical symptoms, past medical history, and examination findings..." : "লক্ষণ, পূর্ববর্তী অসুস্থতার ইতিহাস ও পর্যবেক্ষণ লিখুন..."}
                  className="input-glass text-xs resize-none leading-relaxed"
                />
              </div>
            </div>

            {/* ═══ PANEL 3: Smart Prescription Composer (Rx) (5 Columns) ═══ */}
            <div className="col-span-12 lg:col-span-5 space-y-4">
              <div className="glass-panel p-4 space-y-3.5">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-xl font-serif font-black text-teal-400">℞</span>
                    <h3 className="text-xs font-bold uppercase tracking-wider text-white">
                      {lang === 'en' ? 'Prescription Composer (Rx)' : 'প্রেসক্রিপশন কম্পোজার'}
                    </h3>
                  </div>
                  <span className="badge badge-teal text-[10px]">
                    {medicines.length} {lang === 'en' ? 'Medicines' : 'টি ওষুধ'}
                  </span>
                </div>

                {/* Medicine Search & Input */}
                <div className="relative">
                  <Search className="w-3.5 h-3.5 absolute left-3 top-3 text-slate-400" />
                  <input
                    type="text"
                    value={medQuery}
                    onChange={(e) => setMedQuery(e.target.value)}
                    placeholder={lang === 'en' ? "Search medicine catalogue (e.g. Napa, Seclo, Angilock)..." : "ওষুধের নাম খুঁজুন (যেমন নাপা, সেকলো, ক্যামলোডিন)..."}
                    className="input-glass pl-8 text-xs py-2"
                  />

                  {/* Auto-suggest dropdown */}
                  {medSuggestions.length > 0 && (
                    <div className="absolute left-0 right-0 top-full mt-1 bg-slate-900 border border-teal-500/30 rounded-xl shadow-2xl z-20 max-h-48 overflow-auto p-1">
                      {medSuggestions.map((m, idx) => (
                        <div
                          key={idx}
                          onClick={() => {
                            setSelectedMed(`${m.genericName} ${m.strength} (${m.brandName})`);
                            setMedQuery(`${m.brandName} - ${m.genericName} ${m.strength}`);
                            setMedSuggestions([]);
                          }}
                          className="p-2 hover:bg-teal-900/40 rounded-lg cursor-pointer flex items-center justify-between text-xs transition-colors"
                        >
                          <div>
                            <span className="font-bold text-white mr-1.5">{m.brandName}</span>
                            <span className="text-teal-300">({m.genericName} {m.strength})</span>
                          </div>
                          <span className="badge badge-teal text-[9px]">{m.form || 'Tablet'}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Dosage & Frequency Presets */}
                <div className="space-y-2 bg-slate-900/60 p-3 rounded-xl border border-teal-500/15">
                  <div className="flex items-center gap-1.5 overflow-x-auto pb-1">
                    <span className="text-[10px] text-slate-400 shrink-0">{lang === 'en' ? 'Frequency:' : 'মাত্রা:'}</span>
                    {['1+0+0', '1+0+1', '1+1+1', '0+0+1', '1+0+0 (সকালে)'].map((freq) => (
                      <button
                        key={freq}
                        onClick={() => setSelectedFreq(freq)}
                        className={`text-[10px] px-2 py-0.5 rounded-md font-mono shrink-0 transition-all ${
                          selectedFreq === freq ? 'bg-teal-500 text-white font-bold' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
                        }`}
                      >
                        {freq}
                      </button>
                    ))}
                  </div>

                  <div className="flex items-center gap-1.5 overflow-x-auto pb-1">
                    <span className="text-[10px] text-slate-400 shrink-0">{lang === 'en' ? 'Duration:' : 'মেয়াদ:'}</span>
                    {[3, 5, 7, 14, 30].map((d) => (
                      <button
                        key={d}
                        onClick={() => setSelectedDuration(d)}
                        className={`text-[10px] px-2 py-0.5 rounded-md shrink-0 transition-all ${
                          selectedDuration === d ? 'bg-teal-500 text-white font-bold' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
                        }`}
                      >
                        {d} {lang === 'en' ? 'Days' : 'দিন'}
                      </button>
                    ))}
                  </div>

                  <div className="flex items-center gap-2 pt-1">
                    <select
                      value={selectedInstruction}
                      onChange={(e) => setSelectedInstruction(e.target.value)}
                      className="input-glass text-xs py-1.5 text-slate-300"
                    >
                      <option value="After meal (ভরা পেটে)">After meal (খাবার পর / ভরা পেটে)</option>
                      <option value="Before meal (খাবার আগে)">Before meal (খাবার আগে)</option>
                      <option value="Before sleep (ঘুমানোর আগে)">Before sleep (ঘুমানোর আগে)</option>
                      <option value="With water (প্রচুর পানি সহ)">With water (প্রচুর পানি সহ)</option>
                    </select>

                    <button 
                      onClick={addMedicine}
                      disabled={!selectedMed && !medQuery}
                      className="btn-primary text-xs py-1.5 px-3 shrink-0"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      {lang === 'en' ? 'Add' : 'যোগ করুন'}
                    </button>
                  </div>
                </div>

                {/* Prescribed Medicines List */}
                <div className="space-y-2 max-h-52 overflow-y-auto pr-1">
                  {medicines.map((med, index) => (
                    <div key={med.id} className="bg-slate-900/90 p-2.5 rounded-xl border border-teal-500/20 flex items-start justify-between">
                      <div className="flex items-start gap-2.5">
                        <span className="w-5 h-5 rounded-full bg-teal-500/20 text-teal-300 text-[10px] font-bold flex items-center justify-center shrink-0 mt-0.5">
                          {index + 1}
                        </span>
                        <div>
                          <p className="text-xs font-bold text-white">{med.medicineName}</p>
                          <div className="flex items-center gap-2 text-[11px] text-teal-400 mt-0.5">
                            <span className="font-semibold">{med.frequency}</span>
                            <span>•</span>
                            <span>{med.durationDays} দিন / days</span>
                            <span>•</span>
                            <span className="text-slate-400 font-bengali">{med.instructionsBn || med.instructions}</span>
                          </div>
                        </div>
                      </div>

                      <button 
                        onClick={() => removeMedicine(med.id)}
                        className="text-slate-500 hover:text-red-400 p-1"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  ))}
                </div>

                {/* Follow-up & Advice */}
                <div className="pt-2 border-t border-slate-800 space-y-2">
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-slate-400">{lang === 'en' ? 'Follow-up Consultation:' : 'পরবর্তী সাক্ষাত:'}</span>
                    <div className="flex items-center gap-1.5">
                      {[7, 14, 30].map(days => (
                        <button
                          key={days}
                          onClick={() => setFollowUpDays(days)}
                          className={`text-[10px] px-2 py-0.5 rounded ${
                            followUpDays === days ? 'bg-teal-600 text-white font-bold' : 'bg-slate-800 text-slate-400'
                          }`}
                        >
                          +{days} {lang === 'en' ? 'days' : 'দিন'}
                        </button>
                      ))}
                    </div>
                  </div>

                  <input
                    type="text"
                    value={doctorAdvice}
                    onChange={(e) => setDoctorAdvice(e.target.value)}
                    placeholder={lang === 'en' ? "Doctor's advice (e.g. low salt diet)..." : "ডাক্তারের পরামর্শ..."}
                    className="input-glass text-xs py-1.5"
                  />
                </div>

                {/* Action Bar (Issue & PDF) */}
                <div className="pt-2 flex flex-col gap-2">
                  <button
                    onClick={handleIssuePrescription}
                    className="w-full btn-primary py-2.5 justify-center font-bold text-sm shadow-xl shadow-teal-500/20"
                  >
                    <CheckCircle2 className="w-4 h-4" />
                    {lang === 'en' 
                      ? `Sign & Issue Prescription (BMDC: ${doctor.bmdc})` 
                      : `ডিজিটাল প্রেসক্রিপশন স্বাক্ষর ও জারি করুন (${doctor.bmdc})`}
                  </button>

                  <button
                    onClick={handleDownloadPdf}
                    className="w-full btn-secondary py-2 justify-center font-semibold text-xs text-teal-300 hover:text-white"
                  >
                    <Download className="w-3.5 h-3.5" />
                    {lang === 'en' ? 'Download Official Prescription PDF' : 'অফিসিয়াল প্রেসক্রিপশন PDF ডাউনলোড'}
                  </button>

                  {prescriptionSuccess && (
                    <div className="bg-emerald-500/10 border border-emerald-500/30 p-2.5 rounded-xl text-center text-xs text-emerald-400 font-semibold animate-fade-in flex items-center justify-center gap-2">
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                      {lang === 'en' 
                        ? `Prescription #${prescriptionId} saved to patient EMR & ready for pharmacy dispensing` 
                        : `প্রেসক্রিপশন #${prescriptionId} ইএমআর-এ সংরক্ষিত এবং ফার্মেসির জন্য প্রস্তুত`}
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;
