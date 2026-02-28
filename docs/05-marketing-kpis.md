# Marketing KPIs

**Version:** 1.0 | **Date:** 2026-02-27 | **Status:** Draft

---

## 1. Acquisition & Channel Performance

| KPI | Formula | Data Source | Unit |
|-----|---------|-------------|------|
| **Marketing Qualified Leads (MQLs)** | Count of leads meeting qualification criteria | CRM / Marketing Platform | Count |
| **Sales Qualified Leads (SQLs)** | Count of MQLs accepted by Sales | CRM | Count |
| **MQL-to-SQL Conversion Rate** | SQLs / MQLs * 100 | Calculated | % |
| **Cost per Lead (CPL)** | Total Marketing Spend / Total Leads Generated | Calculated | EUR |
| **Cost per MQL** | Total Marketing Spend / MQLs | Calculated | EUR |
| **Cost per SQL** | Total Marketing Spend / SQLs | Calculated | EUR |
| **Customer Acquisition Cost (Marketing)** | Marketing Spend / New Customers from Marketing | Calculated | EUR |
| **Marketing ROI** | (Revenue Attributed to Marketing - Marketing Spend) / Marketing Spend * 100 | Calculated | % |
| **Channel ROI** | (Channel Revenue - Channel Spend) / Channel Spend * 100 per channel | Calculated | % |
| **Marketing Spend as % of Revenue** | Total Marketing Spend / Total Revenue * 100 | Calculated | % |
| **Marketing Sourced Pipeline** | Pipeline value from marketing-generated leads | CRM | EUR |
| **Marketing Influenced Pipeline** | Pipeline value where marketing touched any stage | CRM | EUR |

### Channel Performance Breakdown

Per channel (configurable, defaults below):

| Channel | KPIs Tracked |
|---------|-------------|
| **Organic Search (SEO)** | Traffic, leads, CPL, conversion rate, keyword rankings |
| **Paid Search (SEM/PPC)** | Impressions, clicks, CTR, CPC, leads, CPL, ROAS |
| **Social Media (Organic)** | Reach, engagement, followers, leads, conversion rate |
| **Social Media (Paid)** | Impressions, clicks, CTR, CPL, ROAS |
| **Email Marketing** | Sends, opens, clicks, unsubscribes, leads, conversion rate |
| **Content Marketing** | Page views, time on page, downloads, leads |
| **Events / Webinars** | Registrations, attendees, leads, pipeline generated |
| **Referral / Partner** | Referrals, qualified leads, conversion rate, revenue |
| **Direct Traffic** | Visits, leads, conversion rate |

### Channel ROI Comparison

```
Channel ROI Report (Monthly):

Channel           Spend      Leads   CPL      Revenue    ROI
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Organic Search    2,000      120     17 EUR   48,000     2,300%
Paid Search       15,000     200     75 EUR   85,000     467%
Social (Paid)     8,000      80      100 EUR  24,000     200%
Email             1,500      60      25 EUR   36,000     2,300%
Events            12,000     40      300 EUR  120,000    900%
Content           3,000      50      60 EUR   15,000     400%
Referral          500        30      17 EUR   45,000     8,900%
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Total             42,000     580     72 EUR   373,000    788%
```

---

## 2. Content & Engagement KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Website Traffic (Unique Visitors)** | Distinct visitors per period | Count |
| **Website Sessions** | Total sessions per period | Count |
| **Pages per Session** | Total Page Views / Sessions | Ratio |
| **Average Session Duration** | Total Session Time / Sessions | Minutes |
| **Bounce Rate** | Single-page sessions / Total Sessions * 100 | % |
| **Traffic-to-Lead Rate** | Leads / Unique Visitors * 100 | % |
| **Content Engagement Rate** | Interactions / Impressions * 100 | % |
| **Email Open Rate** | Opens / Emails Delivered * 100 | % |
| **Email Click-Through Rate (CTR)** | Clicks / Opens * 100 | % |
| **Email Unsubscribe Rate** | Unsubscribes / Emails Delivered * 100 | % |
| **Campaign Conversion Rate** | Conversions / Campaign Reach * 100 | % |
| **Social Media Engagement Rate** | (Likes + Comments + Shares) / Followers * 100 | % |
| **Brand Awareness Index** | Composite of search volume, mentions, direct traffic | Index (0-100) |
| **Share of Voice** | Brand Mentions / Total Category Mentions * 100 | % |

### Content Performance Matrix

```
Content Type    Views    Leads    Conv Rate    CPL       Effort
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Blog Posts      12,000   60       0.5%         10 EUR    Low
Whitepapers     2,000    100      5.0%         30 EUR    Medium
Webinars        500      80       16.0%        150 EUR   High
Case Studies    3,000    45       1.5%         20 EUR    Medium
Videos          8,000    30       0.4%         50 EUR    High
Infographics    5,000    20       0.4%         15 EUR    Low
```

---

## 3. Attribution Models

Clarity Board supports multiple attribution models for revenue assignment to marketing touchpoints:

### Available Models

| Model | Logic | Best For |
|-------|-------|----------|
| **First Touch** | 100% credit to first interaction | Understanding awareness drivers |
| **Last Touch** | 100% credit to last interaction before conversion | Understanding closing drivers |
| **Linear** | Equal credit across all touchpoints | Fair distribution when all touches matter |
| **Time Decay** | More credit to recent touchpoints (configurable half-life) | Long sales cycles |
| **Position-Based (U-shaped)** | 40% first, 40% last, 20% distributed middle | Balanced first/last emphasis |
| **W-shaped** | 30% first, 30% lead creation, 30% opportunity creation, 10% rest | B2B with clear stage transitions |
| **Custom Weighted** | User-configurable weights per channel/touchpoint type | Company-specific knowledge |

### Attribution Example

```
Customer Journey:
  Day 1:  Google Ad Click (Paid Search)
  Day 5:  Blog Post Visit (Content)
  Day 12: Webinar Attendance (Events)
  Day 18: Email Click (Email)
  Day 25: Demo Request (Direct)
  Day 35: Closed Won - 12,000 EUR

Attribution by Model:
  First Touch:    Paid Search = 12,000 EUR
  Last Touch:     Direct = 12,000 EUR
  Linear:         Each = 2,400 EUR (5 touchpoints)
  Time Decay:     Paid=600, Content=1,200, Events=2,400, Email=3,600, Direct=4,200
  Position-Based: Paid=4,800, Content=800, Events=800, Email=800, Direct=4,800
```

### Attribution Configuration

- Default model is configurable per organization
- Side-by-side comparison of models available
- Attribution window: Configurable lookback period (default: 90 days)
- Excluded touchpoints: Ability to exclude specific interactions (e.g., support emails)
- Multi-currency: Attribution values in deal currency, converted to reporting currency

---

## 4. Marketing Funnel Integration

Marketing KPIs feed into the overall business funnel:

```
Marketing Layer                    Sales Layer
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Impressions / Reach
  ↓ (CTR)
Website Visitors
  ↓ (Traffic-to-Lead)
Leads (Raw)
  ↓ (Qualification Rate)
MQLs ─────────────────────→ SQLs
                              ↓ (Acceptance Rate)
                           Opportunities
                              ↓ (Win Rate)
                           Closed Won → Revenue
```

### Marketing-to-Revenue Metrics

| Metric | Formula | Target |
|--------|---------|--------|
| Marketing Contribution to Revenue | Marketing-Sourced Revenue / Total Revenue | > 30% |
| Marketing Contribution to Pipeline | Marketing-Sourced Pipeline / Total Pipeline | > 40% |
| Marketing Efficiency Ratio | Marketing Spend / Marketing-Sourced Revenue | < 0.3 |
| Time from Lead to MQL | Average days | < 14 days |
| Time from MQL to SQL | Average days | < 7 days |

---

## 5. Budget & Spend KPIs

| KPI | Formula | Unit |
|-----|---------|------|
| **Marketing Budget Utilization** | Actual Spend / Budgeted Spend * 100 | % |
| **Cost per Opportunity** | Marketing Spend / Opportunities Created | EUR |
| **Cost per Won Customer** | Marketing Spend / Customers Won | EUR |
| **Return on Ad Spend (ROAS)** | Revenue from Ads / Ad Spend | Ratio |
| **Blended CAC** | (Sales + Marketing Spend) / New Customers | EUR |
| **Organic vs. Paid Ratio** | Organic Leads / Paid Leads | Ratio |

---

## Document Navigation

- Previous: [Sales KPIs](./04-sales-kpis.md)
- Next: [HR KPIs](./06-hr-kpis.md)
- [Back to Index](./README.md)
