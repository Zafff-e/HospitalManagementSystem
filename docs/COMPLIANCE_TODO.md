# Compliance Checklist — TODO Items

> ⚠️ **This document lists regulatory and legal items that MUST be reviewed by qualified local counsel before any real patient deployment.**

## Data Protection & Privacy
- [ ] Confirm data-protection requirements with Bangladesh Data Protection Act (if enacted)
- [ ] Review DGHS (Directorate General of Health Services) guidance on electronic health records
- [ ] Determine if patient data may leave the country (affects cloud hosting location)
- [ ] Confirm data retention and deletion policies with legal counsel
- [ ] Review NID storage and encryption requirements under NID Act

## Health Record Compliance
- [ ] Confirm with DGHS whether electronic prescriptions require specific formatting
- [ ] Verify if BMDC registration number display requirements exist for e-prescriptions
- [ ] Review lab report format requirements with relevant health authorities
- [ ] Confirm whether digital signatures are required on prescriptions/reports

## Payment Gateway
- [ ] Obtain bKash payment gateway sandbox credentials and review terms
- [ ] Obtain Nagad payment gateway sandbox credentials and review terms
- [ ] Obtain Rocket payment gateway credentials if needed
- [ ] Confirm VAT compliance requirements with NBR (National Board of Revenue)
- [ ] Review card payment PCI-DSS compliance requirements

## SMS & Communications
- [ ] Select and contract with a local bulk-SMS provider
- [ ] Review regulations around unsolicited SMS (BTRC guidelines)
- [ ] Confirm OTP-based authentication requirements

## Hosting & Deployment
- [ ] Determine hosting location (in-country vs. international cloud)
- [ ] Review BTRC regulations on hosting healthcare data
- [ ] Confirm backup and disaster recovery requirements
- [ ] Review uptime and availability requirements for hospitals

## Licensing & Business
- [ ] Confirm software licensing model (SaaS vs. on-premise)
- [ ] Review employment law implications for HR/payroll module
- [ ] Confirm tax calculation rules with qualified accountant

---

**Note:** This software is a product. It does NOT claim legal compliance with any specific regulation. Compliance is the responsibility of the deploying organization and must be confirmed by qualified legal counsel.
